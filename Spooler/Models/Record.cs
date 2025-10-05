
using Newtonsoft.Json;
using Spooler.HKA;
using Spooler.Ticket;
using System;
using System.Reflection;
using System.Threading;

namespace Spooler.Models
{
    public class Record
    {

        public int Id { get; set; }
   
        public String JasonValue { get; set; }
        
        public Document Document  { get; set; }
      
        public BaseDocument BaseDocument { get; set; }

        public String Reference { get; set; }
        
        public String Printer_Serial { get; set; }

        public typeDocument Type { get; set; }

        public bool Reprint { get; set; }

        public bool Printed { get; set; }

        public string DocumentNumber { get; set; }



        public void ConvertJasonValueToDocument()
        {
            try
            {
                if (JasonValue != null)
                {
                    this.Document = new Document();
                    this.Document = JsonConvert.DeserializeObject<Document>(JasonValue);
                }

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
        }

        public void ConvertJasonValueToBaseDocument()
        {
            try
            {
                if (JasonValue != null)
                {
                    this.BaseDocument = new BaseDocument();
                    this.BaseDocument = JsonConvert.DeserializeObject<BaseDocument>(JasonValue);
                }

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
        }

        private bool PrinterActivated()
        {
            begining:
            //Printer _printer = new Printer(Spooler.Parameters.Port);
            bool printerOk = false;

            try
            {
                while (!printerOk)
                {
                    printerOk = (Printer.PrinterOn() && Printer.PrinterOnLine()) ? true : false;    
                    if (!printerOk)
                        Printer.ClosePort();
                    Thread.Sleep(500);
                }

                return true;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                Thread.Sleep(500);
                goto begining;
            }


        }

        private bool SendInvoice()
        {
            try
            {
                //Printer _printer = new Printer(Spooler.Parameters.Port);

                double _missingAmount = 0;
                Printer.MissingAmount(ref _missingAmount);

                bool _paymentsMade = false;
                _paymentsMade = Printer.ExistsPaymentsMade();

                if (_paymentsMade || _missingAmount > 0) 
                {
                    Printer.CancelInvoice();
                    Spooler.Models.Logger.loggerMessege("Advertencia: Ejecutando Printer.CancelInvoice() al iniciar SendInvoice");
                    if (_paymentsMade)
                        return true; // factura pendiente que si saldra
                }

                bool _openedInvoice = false;
                _openedInvoice = Printer.OpenInvoice(Document);
                if (_openedInvoice)
                {
                    foreach (Product _product in Document.Products)
                    {
                        Printer.SendItemInvoice(_product.Name, _product.Tax, _product.Quantity, _product.Price);
                        Printer.SendComment(_product.Comment);
                    }

                    Printer.CloseInvoice();

                    if (Document.Discount>0)
                        Printer.SendDiscount(Document.Discount);

                    foreach (Payment _payment in Document.Payments)
                    {
                        Printer.SenPayment(_payment.Code, _payment.Amount);
                    }

                    // desde aqui si pierde la comunicacion y se recupera, la factura si sale
                    double _missingAmount2 = 0;
                    Printer.MissingAmount(ref _missingAmount2);

                    if (_missingAmount2 > 0)
                        Printer.SenPayment("1", Convert.ToDecimal(_missingAmount2));

                    if (Spooler.Parameters.Igtf)
                        Printer.CloseIgtfInvoice();

                    double _missingAmount3 = 0;
                    Printer.MissingAmount(ref _missingAmount3);

                    bool _paymentsMade2 = false;
                    _paymentsMade2 = Printer.ExistsPaymentsMade();

                    if (_paymentsMade2 || _missingAmount3 > 0)
                    {
                        Printer.CancelInvoice();
                        Spooler.Models.Logger.loggerMessege("Advertencia: Ejecutando Printer.CancelInvoice() al terminar SendInvoice");
                        if (_paymentsMade2)
                            return true; // factura pendiente que si saldra
                        else
                            return false; // factura anulada que no saldra
                    }
                    else
                        return true; // factura impresa

                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool PrintInvoice(Record _lastInvoice)
        {
            try
            {
                ConvertJasonValueToDocument();
                if (this.Document != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer Printer = new Printer(Spooler.Parameters.Port);                     
                        int _before = Printer.LastInvoiceNumber();                       

                        if (_before > 0)
                        {
                            int _lastInvoiceNumberInSpooler = 0;
                            int.TryParse(_lastInvoice.DocumentNumber, out _lastInvoiceNumberInSpooler);

                            // caso en que la factura impresa anteriormente no se grabo en bd
                            if (_before > _lastInvoiceNumberInSpooler)
                            {
                                Spooler.Models.Logger.loggerMessege("Advertencia: Factura impresa que no esta en BD: "+ _before.ToString());
    
                                this.DocumentNumber = _before.ToString();
                                this.Printed = true;
                                return true;
                                
                            }                                

                        }

                        // caso en que volvio a entrar un id ya impreso!
                        if (this.Id == _lastInvoice.Id)
                            return false;

                        bool _printedInvoice = SendInvoice();
                        Thread.Sleep(500);
                        int _after = Printer.LastInvoiceNumber();

                        if (((_before != _after) && (_after > 0)) && _printedInvoice) //  if (((_before != _after) && (_after > 0)) || _printedInvoice)
                        {
                            Console.WriteLine("Info: Printing Invoice, Id: " + Id.ToString());
                            this.DocumentNumber  = _after.ToString();
                            this.Printed = true;
                            Printer.ClosePort();
                            return true;
                        }
                        else
                            return false;
                    } 
                    else
                        return false; 
                }
                else
                    return false;
     
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }
        }

        private bool SendRefund()
        {

            try
            {
                //Printer Printer = new Printer(Spooler.Parameters.Port);
                bool _openedRefund = false;
                _openedRefund = Printer.OpenRefund(
                    Document.Client.Vat, 
                    Document.Client.Name, 
                    Document.BillNumber.ToString(), 
                    Document.BillDate, 
                    Document.Printer_Serial, 
                    "");
               
                if (_openedRefund)
                {
                    foreach (Product _product in Document.Products)
                    {
                        Printer.SendItemRefund(_product.Name, _product.Tax, _product.Quantity, _product.Price);
                    }

                    Printer.CloseRefund();

                    if (Document.Discount > 0)
                        Printer.SendDiscount(Document.Discount);

                    foreach (Payment _payment in Document.Payments)
                    {
                        Printer.SenPayment(_payment.Code, _payment.Amount);
                    }

                    if (Spooler.Parameters.Igtf)
                        Printer.CloseIgtfInvoice();

                    Printer.ClosePort();
                    return true;

                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool PrintRefund()
        {
            try
            {
                ConvertJasonValueToDocument();
                if (this.Document != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer Printer = new Printer(Spooler.Parameters.Port);
                        int _before = Printer.LastCreditNoteNumber();
                        bool _printedRefund= SendRefund();
                        int _after = Printer.LastCreditNoteNumber();

                        if (((_before != _after) && (_after > 0)) || _printedRefund)
                        {
                            Console.WriteLine("Info: Printing Refund, Id: " + Id.ToString());
                            this.DocumentNumber = _after.ToString();
                            this.Printed = true;
                            Printer.ClosePort();
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool PrintXReport()
        {
            try
            {
                ConvertJasonValueToBaseDocument();
                if (this.BaseDocument != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer Printer = new Printer(Spooler.Parameters.Port);
                        Printer.PrintXReport();
                        this.DocumentNumber = "";
                        this.Printed = true;
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool PrintZReport()
        {
            try
            {

                ConvertJasonValueToBaseDocument();
                if (this.BaseDocument != null)
                {
                    if (PrinterActivated())
                    {
                        this.DocumentNumber = "";
                        //Printer Printer = new Printer(Spooler.Parameters.Port);
                        int _before = Printer.LastZReportNumber();
                        Printer.PrintZReport();
                        int _after = Printer.LastZReportNumber();

                        if ((_before != _after) && (_after > 0))
                        {
                            Console.WriteLine("printing Z Report, Id: " + Id.ToString());
                            this.DocumentNumber = _after.ToString();
                            this.Printed = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        return false;
                }
                else
                    return false;


            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool RePrintZReport()
        {
            try
            {
                ConvertJasonValueToBaseDocument();
                if (this.BaseDocument != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer _printer = new Printer(Spooler.Parameters.Port);
                        if (Printer.RePrintZReport(BaseDocument.Number))
                        {
                            Console.WriteLine("reprinting Z Report, Id: " + Id.ToString());
                            this.DocumentNumber = BaseDocument.Number.ToString();
                            this.Printed = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool RePrintInvoice()
        {
            try
            {
                ConvertJasonValueToBaseDocument();
                if (this.BaseDocument != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer _printer = new Printer(Spooler.Parameters.Port);
                        if (Printer.RePrintInvoice(BaseDocument.Number))
                        {
                            Console.WriteLine("reprinting invoice, Id: " + Id.ToString());
                            this.DocumentNumber = BaseDocument.Number.ToString();
                            this.Printed = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool RePrintRefund()
        {
            try
            {
                ConvertJasonValueToBaseDocument();
                if (this.BaseDocument != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer _printer = new Printer(Spooler.Parameters.Port);
                        if (Printer.RePrintRefund(BaseDocument.Number))
                        {
                            Console.WriteLine("reprinting refund, Id: " + Id.ToString());
                            this.DocumentNumber = BaseDocument.Number.ToString();
                            this.Printed = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool PrintPayMethods()
        {
            try
            {
                ConvertJasonValueToBaseDocument();
                if (this.BaseDocument != null)
                {
                    if (PrinterActivated())
                    {
                        //Printer _printer = new Printer(Spooler.Parameters.Port);
                        Printer.PrintPayMethods();
                        this.DocumentNumber = "";
                        this.Printed = true;
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }

        }

        public bool PrintTicket(string _printerName)
        {
            try
            {
                //ConvertJasonValueToDocument();
                Spooler.Models.Document _tiket = new Spooler.Models.Document();
                _tiket = JsonConvert.DeserializeObject<Document>(JasonValue);

                if (_tiket != null)
                {
                    string _docNumber = _tiket.Reference;
                    if (_tiket.BillNumber != "")
                    {
                        _docNumber = _tiket.BillNumber;
                    }
                    else
                    {
                        _tiket.BillNumber = _tiket.Reference;
                    }

                    if (_tiket.Text == "")
                    {
                        frmTicket _frmticket = new frmTicket();
                        _frmticket.PrintTicket(_tiket, _printerName);
                    }
                    else
                    {
                        frmTicketlibre _frmticket = new frmTicketlibre();
                        _frmticket.PrintTicket(_tiket, _printerName);
                    }          
                                
                    this.DocumentNumber = _docNumber;
                    this.Printed = true;

                }
                return true;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }
        }


    }
}
