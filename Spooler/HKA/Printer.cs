using System;
using System.Linq;
using System.Threading;
using TfhkaNet.IF.VE;
using TfhkaNet.IF;
using System.Xml.Linq;
using System.Diagnostics.SymbolStore;
using Microsoft.Reporting.Map.WebForms.BingMaps;
using static System.Net.Mime.MediaTypeNames;
using Spooler.Models;

namespace Spooler.HKA
{
    public static class Printer
    {

        private static Tfhka _printer = new Tfhka();
        private static string Port { get; set; } = "";
        private static string Flag { get; set; } = "";

        private static PrinterStatus StatusError;
        private static S1PrinterData ReporteS1;
        private static S2PrinterData ReporteS2;
        private static S3PrinterData ReporteS3;


        public static void SetPort(string _myport)
        {
            Port = _myport;
        }

        private static string GetFlag()
        {
            string _flag = "";

            try
            {

                if (Flag == "")
                {
                    S3PrinterData _ReporteS3 = _printer.GetS3PrinterData();
                    _flag = _ReporteS3.AllSystemFlags[21].ToString();
                }
                else
                {
                    _flag = int.Parse(Flag).ToString();
                }

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
            }

            return _flag;
        }

        public static bool OpenPort()
        {
            bool _opened = false;

        beginingOpenPort:

            try
            {

                if (_printer.StatusPort)
                {
                    Flag = String.Format("{0:00}", int.Parse(GetFlag()));
                    if (Flag == "")
                        throw new Exception("Error: in GetFlag");

                    _opened = true;
                }
                else
                {
                    bool _repuesta = _printer.OpenFpCtrl(Port);

                    if (_repuesta)
                    {
                        Flag = String.Format("{0:00}", int.Parse(GetFlag()));
                        if (Flag == "")
                            throw new Exception("Error: in GetFlag");
                        _opened = true;
                    }
                    else
                    {
                        _opened = false;
                        _printer.CloseFpCtrl();
                        throw new Exception("Error: in OpenPort, Port closed. OpenFpCtrl is false");
                    }
                }

            }
            catch (Exception ex)
            {
                _opened = false;
                Spooler.Models.Logger.loggerMessege(ex);
                _printer.CloseFpCtrl();
                Thread.Sleep(5000);
                goto beginingOpenPort;
            }

            return _opened;
        }

        public static bool PrinterOn()
        {

        BeginingPrinterOn:

            try
            {
                if (OpenPort())
                {
                    bool _result = _printer.CheckFPrinter();
                    if (!_result)
                        throw new Exception("Error: printer is NOT ON!...");
                }
                else
                {
                    _printer.CloseFpCtrl();
                    Thread.Sleep(5000);
                    goto BeginingPrinterOn;
                }
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                _printer.CloseFpCtrl();
                Thread.Sleep(5000);
                goto BeginingPrinterOn;
            }

            return true;

        }

        public static bool ClosePort()
        {
            try
            {
                if (_printer.StatusPort)
                {
                    _printer.CloseFpCtrl();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        private static bool SendCommand(string _command, bool waitSuccessfulCommand = true, bool repeatAttempts = true)
        {
            try
            {
                var _firstAttempt = true;
                int _attempts;
                int _TopAttempts;
                _attempts = 1;
                _TopAttempts = 10;


            beginingCheckPort:
                ;
                try
                {
                    if (!OpenPort())
                        throw new Exception("Error: in SendCommand. Port closed");
                }
                catch (Exception ex)
                {
                    Spooler.Models.Logger.loggerMessege(ex);
                    Thread.Sleep(500);
                    goto beginingCheckPort;
                }


            beginingCheckPrinter:
                ;
                try
                {
                    if (!PrinterOn())
                        throw new Exception("Error: in SendCommand. Printer disabled");
                    _attempts = 1;
                }
                catch (Exception ex)
                {
                    Spooler.Models.Logger.loggerMessege(ex);
                    Thread.Sleep(500);
                    if (_attempts <= _TopAttempts)
                    {
                        _attempts = _attempts + 1;
                        goto beginingCheckPrinter;
                    }
                    else
                    {
                        _attempts = 1;
                        goto beginingCheckPort;
                    }
                }


            beginingSendCommand:
                bool result = false;
                try
                {
                    result = _printer.SendCmd(_command);

                    string value = "";
                    if (result)
                        value = "result yes";
                    else
                        value = "result no";

                    StatusError = _printer.GetPrinterStatus();
                    if ((StatusError.PrinterErrorCode > 1))
                    {
                        result = false;
                        throw new Exception("Error: in SendCommand. PrinterErrorCode: " + System.Convert.ToString(StatusError.PrinterErrorCode) + " -- " + value);
                    }

                    _attempts = 1;
                }
                catch (Exception ex)
                {
                    Spooler.Models.Logger.loggerMessege(ex);
                    Thread.Sleep(500);

                    if (repeatAttempts)
                    {
                        if (_attempts <= _TopAttempts)
                        {
                            _attempts = _attempts + 1;
                            goto beginingSendCommand;
                        }
                        else if (!waitSuccessfulCommand)
                        {
                            result = false;
                            if (_firstAttempt)
                            {
                                _firstAttempt = false;
                                _attempts = 1;
                                goto beginingCheckPort;
                            }
                        }
                        else
                        {
                            _attempts = 1;
                            goto beginingCheckPort;
                        }
                    }
                    else
                        result = false;
                }

                return result;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        private static string LenString(string expresion, int start, int characters)
        {
            int ll = 0;
            if ((expresion.Length - start) <= characters)
            {
                ll = (expresion.Length - start);
            }
            else
            {
                ll = characters;
            }
            return expresion.Substring(start, ll);
        }

        public static bool OpenRefund(string vat, string name, string numberInvoice, string dateInvoice, string serial, string note = "")
        {
            try
            {
                SendCommand("iR*" + vat, false, false);
                SendCommand("iS*" + LenString(name, 0, 40), false, false);

                if (name.Length > 40)
                    SendCommand("i01" + LenString(name, 40, 40), false, false);
                if (name.Length > 80)
                    SendCommand("i02" + LenString(name, 80, 40), false, false);

                SendCommand("iF*" + numberInvoice, false, false);
                SendCommand("iD*" + dateInvoice, false, false); // 01/02/2024
                SendCommand("iI*" + serial, false, false);

                if (note != "")
                    SendCommand("i03Observacion: " + note, false, false);

                return true;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool OpenInvoice(Document _document)
        {
            try
            {
                int i = 0;
                int ii = 0;
                foreach (Note _note in _document.Notes)
                {
                    if (_note.InFooter)
                    {
                        i++;
                        Printer.PrintNoteMessege(i, _note.Text, _note.InFooter);
                    }
                    else
                    {
                        ii++;
                        Printer.PrintNoteMessege(ii, _note.Text, _note.InFooter);
                    }


                }

                string vat = _document.Client.Vat;
                string name = _document.Client.Name;
                string address = _document.Client.Address;

                SendCommand("PH06", false, false);
                SendCommand("PH91", false, false);
                SendCommand("iR*" + vat, false, false);
                SendCommand("iS*" + LenString(name, 0, 40), false, false);

                if (name.Length > 40)
                    SendCommand("i01" + LenString(name, 40, 40), false, false);
                if (name.Length > 80)
                    SendCommand("i02" + LenString(name, 80, 40), false, false);

                if (address != "")
                {
                    SendCommand("i03DIRECCION: " + LenString(address, 0, 30), false, false);
                    if (address.Length > 30)
                        SendCommand("i04" + LenString(address, 30, 40), false, false);
                    if (address.Length > 70)
                        SendCommand("i05" + LenString(address, 70, 40), false, false);
                    if (address.Length > 110)
                        SendCommand("i06" + LenString(address, 110, 40), false, false);
                }

                return true;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        private static string StringAddItem(int quantity, string name, decimal price, string stringTax, string flag)
        {
            switch (flag)
            {
                case "00":// monto 10 caracteres 8 enteros y 2 decimales / cantidad 8 caracteres 5 enteros y 3 decimales
                    return string.Format("{0}{1:00000000}{2:00}{3:00000}{4:000}{5}", stringTax, (long)Convert.ToInt64(Math.Truncate(price)), ((price - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(price)))) * 100), quantity, 0, name);
                case "01": // monto 10 caracteres 7 enteros y 3 decimales / cantidad 8 caracteres 5 enteros y 3 decimales
                    return string.Format("{0}{1:0000000}{2:000}{3:00000}{4:000}{5}", stringTax, (long)Convert.ToInt64(Math.Truncate(price)), ((price - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(price)))) * 100), quantity, 0, name);
                case "02": // monto 10 caracteres 6 enteros y 4 decimales / cantidad 8 caracteres 5 enteros y 3 decimales
                    return string.Format("{0}{1:000000}{2:0000}{3:00000}{4:000}{5}", stringTax, (long)Convert.ToInt64(Math.Truncate(price)), ((price - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(price)))) * 100), quantity, 0, name);
                case "11": // monto 10 caracteres 9 enteros y 1 decimales / cantidad 8 caracteres 5 enteros y 3 decimales
                    return string.Format("{0}{1:000000000}{2:0}{3:00000}{4:000}{5}", stringTax, (long)Convert.ToInt64(Math.Truncate(price)), ((price - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(price)))) * 100), quantity, 0, name);
                case "12": // monto 10 caracteres 10 enteros y 0 decimales / cantidad 8 caracteres 5 enteros y 3 decimales
                           // Return String.Format("{0}{1:00000000}{2:00}{3:00000}{4:000}{5}", caracterAlicuota, CLng(Convert.ToInt64(Math.Truncate(precio))), 0, cantidad, 0, descripcion)
                           // Return String.Format("{0}{1:0000000000}{2:00000}{3:000}{4}", caracterAlicuota, CLng(Convert.ToInt64(Math.Truncate(precio))), cantidad, 0, descripcion)
                    return string.Format("{0}{1:0000000000}{2:00000}{3:000}{4}", stringTax, price, quantity, 0, name);
                case "30": // monto 16 caracteres 14 enteros y 2 decimales / cantidad 17 caracteres 14 enteros y 3 decimales
                    return string.Format("{0}{1:00000000000000}{2:00}{3:00000000000000}{4:000}{5}", stringTax, (long)Convert.ToInt64(Math.Truncate(price)), ((price - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(price)))) * 100), quantity, 0, name);
                default:
                    return string.Format("{0}{1:00000000}{2:00}{3:00000}{4:000}{5}", stringTax, (long)Convert.ToInt64(Math.Truncate(price)), ((price - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(price)))) * 100), quantity, 0, name);
            }
        }

        private static string GetCommandAddItem(int quantity, string name, decimal price, string tax, bool _isInvoice = true)
        {
            string stringcommand = "";
            string stringTax = "";

            if (tax == "E")
                stringTax = (_isInvoice) ? " " : "d0";
            else if (tax == "G")
                stringTax = (_isInvoice) ? "!" : "d1";
            else if (tax == "R")
                stringTax = (_isInvoice) ? Convert.ToChar(34).ToString() : "d2";
            else if (tax == "X")
                stringTax = (_isInvoice) ? "#" : "d3";

            name = LenString(name, 0, 37);
            stringcommand = StringAddItem(quantity, name, price, stringTax, Flag);

            return stringcommand;
        }

        private static bool GetPrinterData(int reportNumber)
        {
            try
            {


            beginingCheckPort:

                try
                {
                    if (!OpenPort())
                        throw new Exception("Error: in GetPrinterData. Port closed");
                }
                catch (Exception ex)
                {
                    Spooler.Models.Logger.loggerMessege(ex);
                    Thread.Sleep(1000);
                    goto beginingCheckPort;
                }


                try
                {
                    if (!PrinterOn())
                        throw new Exception("Error al ObtenerPrinterData. Impresora desactivada");
                }
                catch (Exception ex)
                {
                    Spooler.Models.Logger.loggerMessege(ex);
                    Thread.Sleep(1000);
                    goto beginingCheckPort;
                }

                try
                {
                    if (reportNumber == 1)
                        ReporteS1 = _printer.GetS1PrinterData();
                    if (reportNumber == 2)
                        ReporteS2 = _printer.GetS2PrinterData();
                    if (reportNumber == 3)
                        ReporteS3 = _printer.GetS3PrinterData();

                }
                catch (Exception ex)
                {
                    Spooler.Models.Logger.loggerMessege(ex);
                    Thread.Sleep(1000);
                    goto beginingCheckPort;
                }


                return true;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool MissingAmount(ref double paymentAmount)
        {
            try
            {
                paymentAmount = 0;

                if (GetPrinterData(2))
                {
                    paymentAmount = ReporteS2.AmountPayable;
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                paymentAmount = 0;
                return false;
            }
        }

        public static bool SendItemInvoice(string name, string tax, int quantity, decimal price)
        {
            try
            {

                bool result = false;
                double before = 0;
                double after = 0;

                string comando = GetCommandAddItem(quantity, name, price, tax, true);
                MissingAmount(ref before);

            beginingSendCommand:

                result = false;
                result = SendCommand(comando, true, false);
                MissingAmount(ref after);

                if (before != after)
                    result = true;
                else
                {
                    Thread.Sleep(500);
                    goto beginingSendCommand;
                }

                return result;
            }

            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool SendItemRefund(string name, string tax, int quantity, decimal price)
        {
            try
            {
                string comando = GetCommandAddItem(quantity, name, price, tax, false);
                _printer.SendCmd(comando);

                return true;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool CloseRefund()
        {
            try
            {

                bool result = false;
                result = SendCommand("3", false, false);

                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool CloseInvoice()
        {
            try
            {

                bool result = false;
                result = SendCommand("3", false, false);

                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        private static string GetCommandAddPayment(string paymentCode, decimal paymentAmount, string flag)
        {

            switch (flag)
            {
                case "00": // monto 12 caracteres 10 enteros y 2 decimales 
                    return string.Format("{0}{1:00}{2:0000000000}{3:00}", 2, int.Parse(paymentCode), (long)Convert.ToInt64(Math.Truncate(paymentAmount)), ((paymentAmount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(paymentAmount)))) * 100));

                case "01": // monto 12 caracteres 10 enteros y 2 decimales 
                    return string.Format("{0}{1:00}{2:0000000000}{3:00}", 2, int.Parse(paymentCode), (long)Convert.ToInt64(Math.Truncate(paymentAmount)), ((paymentAmount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(paymentAmount)))) * 100));

                case "02": // monto 12 caracteres 10 enteros y 2 decimales 
                    return string.Format("{0}{1:00}{2:0000000000}{3:00}", 2, int.Parse(paymentCode), (long)Convert.ToInt64(Math.Truncate(paymentAmount)), ((paymentAmount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(paymentAmount)))) * 100));

                case "11": // monto 12 caracteres 11 enteros y 1 decimales 
                    return string.Format("{0}{1:00}{2:00000000000}{3:0}", 2, int.Parse(paymentCode), (long)Convert.ToInt64(Math.Truncate(paymentAmount)), ((paymentAmount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(paymentAmount)))) * 100));

                case "12": // monto 12 caracteres 12 enteros y 0 decimales 
                    return string.Format("{0}{1:00}{2:000000000000}", 2, int.Parse(paymentCode), paymentAmount);

                case "30": // monto 17 caracteres 15 enteros y 2 decimales 
                    return string.Format("{0}{1:00}{2:000000000000000}{3:00}", 2, int.Parse(paymentCode), (long)Convert.ToInt64(Math.Truncate(paymentAmount)), ((paymentAmount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(paymentAmount)))) * 100));

                default:
                    return string.Format("{0}{1:00}{2:0000000000}{3:00}", 2, int.Parse(paymentCode), (long)Convert.ToInt64(Math.Truncate(paymentAmount)), ((paymentAmount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(paymentAmount)))) * 100));
            }
        }

        public static bool SenPayment(string paymentCode, decimal paymentAmount)
        {
            try
            {
                bool result = false;
                string command = "";

                if (paymentAmount > 0)
                {
                    command = GetCommandAddPayment(paymentCode, paymentAmount, Flag);
                }
                else return false;


                double before = 0;
                double after = 0;
                MissingAmount(ref before);

                if (before > 0)
                {
                    result = false;
                    result = SendCommand(command, false);

                    if (result)
                        return true;
                    else
                    {
                        MissingAmount(ref after);

                        if (before != after)
                            return true;
                        else if (after == 0)
                            return true;
                        else
                            return false;
                    }
                }
                else
                    return true;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool CloseIgtfInvoice()
        {
            try
            {
                bool result = false;
                result = SendCommand("199", false); // subtotal
                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool PrintNoteMessege(int _id, string _text, bool _inFooter)
        {
            try
            {
                bool result = false;
                _text = LenString(_text, 0, 40);
                string _codeNote = "";

                if (_id == 1)
                    _codeNote = "i07";
                if (_id == 2)
                    _codeNote = "i08";
                if (_id == 3)
                    _codeNote = "i09";

                //if (_inFooter)
                //{
                //    if (_idPh == 1)
                //        _codPH = "PH91";
                //    if (_idPh == 2)
                //        _codPH = "PH92";
                //    if (_idPh == 3)
                //        _codPH = "PH93";
                //    if (_idPh == 4)
                //        _codPH = "PH94";
                //    if (_idPh == 5)
                //        _codPH = "PH95";
                //}
                //else
                //{
                //    if (_idPh == 1)
                //        _codPH = "PH01";
                //    if (_idPh == 2)
                //        _codPH = "PH02";
                //    if (_idPh == 3)
                //        _codPH = "PH03";
                //    if (_idPh == 4)
                //        _codPH = "PH04";
                //    if (_idPh == 5)
                //        _codPH = "PH05";
                //}

                if ((_id > 0) && (_id < 6))
                {
                    result = SendCommand(_codeNote + _text, false);
                }

                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool SendDiscount(decimal _discount)
        {
            try
            {
                bool result = false;
                string _cmd = string.Format("{0}{1:00}{2:00}", "p-", (long)Convert.ToInt64(Math.Truncate(_discount)), ((_discount - System.Convert.ToDecimal(Convert.ToInt64(Math.Truncate(_discount)))) * 100));
                result = SendCommand(_cmd, false);
                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }




        public static bool CancelInvoice()
        {

            try
            {
                bool result = false;
                int attempts = 1;

                while (attempts <= 10)
                {
                    result = SendCommand("7", false);
                    if (result)
                        break;
                    else
                        attempts++;
                    Thread.Sleep(500);
                }

                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool ExistsPaymentsMade()
        {
            try
            {
                if (GetPrinterData(2))
                {
                    if (ReporteS2.NumberPaymentsMade > 0)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }

            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool PrinterOnLine()
        {
        BeginingPrinterOnLine:

            try
            {
                if (OpenPort())
                {
                    StatusError = _printer.GetPrinterStatus();
                    if (StatusError.PrinterErrorCode > 0)
                        throw new Exception("Error: Printer is NOT on Line!...");
                }
                else
                {
                    _printer.CloseFpCtrl();
                    Thread.Sleep(5000);
                    goto BeginingPrinterOnLine;
                }
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                _printer.CloseFpCtrl();
                Thread.Sleep(5000);
                goto BeginingPrinterOnLine;
            }

            return true;

        }

        public static string GetSerialNumber()
        {
            try
            {
                string serial = "";
                if (OpenPort())
                {
                    ReporteS1 = _printer.GetS1PrinterData();
                    serial = ReporteS1.RegisteredMachineNumber;
                }
                return serial;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return "";
            }

        }

        public static bool PrintXReport()
        {
            try
            {
                if (OpenPort())
                {
                    _printer.PrintXReport();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool PrintZReport()
        {
            try
            {
                if (OpenPort())
                {
                    _printer.PrintZReport();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool RePrintZReport(string ZReportNumber)
        {
            try
            {
                int number = 0;
                if (!int.TryParse(ZReportNumber, out number))
                {
                    return false;
                }

                if (OpenPort())
                {
                    _printer.PrintZReport(number, number);
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool RePrintInvoice(string InvoiceNumber)
        {
            try
            {
                int number = 0;
                if (!int.TryParse(InvoiceNumber, out number))
                {
                    return false;
                }

                if (OpenPort())
                {
                    String rfaccmd = "";
                    rfaccmd = "RF" + number.ToString("D7") + number.ToString("D7");
                    _printer.SendCmd(rfaccmd);
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool RePrintRefund(string RefundNumber)
        {
            try
            {
                int number = 0;
                if (!int.TryParse(RefundNumber, out number))
                {
                    return false;
                }

                if (OpenPort())
                {
                    String rfaccmd = "";
                    rfaccmd = "RC" + number.ToString("D7") + number.ToString("D7");
                    _printer.SendCmd(rfaccmd);
                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static int LastZReportNumber()
        {
            try
            {
                if (GetPrinterData(1))
                {
                    return ReporteS1.DailyClosureCounter;

                }
                else
                    return 0;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return 0;
            }
        }

        public static int LastInvoiceNumber()
        {
            try
            {
                if (GetPrinterData(1))
                {
                    return ReporteS1.LastInvoiceNumber;
                }
                else
                    return 0;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return 0;
            }
        }

        public static int LastCreditNoteNumber()
        {
            try
            {
                if (GetPrinterData(1))
                {
                    return ReporteS1.LastCreditNoteNumber;
                }
                else
                    return 0;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return 0;
            }
        }

        public static bool PrintPayMethods()
        {
            try
            {
                bool result = false;
                result = SendCommand("D", false);
                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }

        public static bool SendComment(string _comment)
        {
            try
            {
                // ESTO DA ERROR. NO SE USA. REVISAR LOGICA
                bool result = false;
                result = SendCommand("@" + LenString(_comment, 0, 20), false);
                return result;
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex);
                return false;
            }
        }


    }
}
