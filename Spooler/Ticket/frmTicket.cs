
using Microsoft.Reporting.Map.WebForms.BingMaps;
using Newtonsoft.Json;
using Npgsql;
using Spooler.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using System.IO;
using System.Drawing.Printing;
using System.Diagnostics;
using GemBox.Pdf;


namespace Spooler.Ticket
{
    public partial class frmTicket : System.Windows.Forms.Form
    {
        private Document _document = new Document();
        public NpgsqlConnection _conection = new NpgsqlConnection();

        public frmTicket()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            this.reportViewer1.RefreshReport();
        }

        public bool Connect()
        {
            try
            {
                if (ConfigurationManager.ConnectionStrings.Count > 0)
                {
                    _conection.ConnectionString = Spooler.Parameters.ConnectionString;
                    _conection.Open();
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

        public void LoadDocument(string _serial)
        {

            try
            {
                _document = new Document();
                if (Connect())
                {
                    NpgsqlCommand _cmd = new NpgsqlCommand();
                    _cmd.CommandText = "select * from getline(:v_printer_serial)";
                    _cmd.Parameters.AddWithValue("@v_printer_serial", NpgsqlTypes.NpgsqlDbType.Varchar, _serial);
                    _cmd.CommandType = CommandType.Text;
                    _cmd.Connection = _conection;
                    NpgsqlDataReader dr = _cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        Record _record = new Record();
                        _record.Id = dr.GetInt32(dr.GetOrdinal("id"));
                        _record.JasonValue = dr.GetString(dr.GetOrdinal("document_json"));
                        _record.Reference = dr.GetString(dr.GetOrdinal("document_reference"));
                        _record.Printer_Serial = dr.GetString(dr.GetOrdinal("printer_serial"));
                        _record.Type = (typeDocument)dr.GetInt32(dr.GetOrdinal("document_type"));
                        _record.Reprint = dr.GetBoolean(dr.GetOrdinal("reprint"));
                        _record.Document = new Document();
                        _record.BaseDocument = new BaseDocument();

                        if (_record.Type == typeDocument.Ticket || _record.Type == typeDocument.Invoice)
                            if (_document.Client.Vat == null)
                                _document = JsonConvert.DeserializeObject<Document>(_record.JasonValue);

                    }
                    _conection.Close();
                }
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }

        }

        public void PrintTicket( Spooler.Models.Document _documentToPrint, string _printerName )
        {
            try
            {
                documentBindingSource.DataSource = _documentToPrint;
                clientBindingSource.DataSource = _documentToPrint.Client;
                productBindingSource.DataSource = _documentToPrint.Products;
                noteBindingSource.DataSource = _documentToPrint.Notes;
                totalBindingSource.DataSource = _documentToPrint.Totals;
                this.reportViewer1.RefreshReport();

                //string mimeType;
                string encoding;
                //string fileNameExtension;
                string[] streamIds;
                string contentType;
                string extension;
                Microsoft.Reporting.WinForms.Warning[] warnings;

                reportViewer1.LocalReport.Refresh();

                string deviceInfo = @"<DeviceInfo>
                      <OutputFormat>EMF</OutputFormat>
                      <PageWidth>4.5in</PageWidth>
                      <PageHeight>11in</PageHeight>
                      <MarginTop>0.1in</MarginTop>
                      <MarginLeft>0.1in</MarginLeft>
                      <MarginRight>0.1in</MarginRight>
                      <MarginBottom>0.1in</MarginBottom>
                    </DeviceInfo>";

                //Export the RDLC Report to Byte Array.
                byte[] pdfBytes = reportViewer1.LocalReport.Render("PDF", deviceInfo, out contentType, out encoding,out extension, out streamIds, out warnings);

                string tempFilePath = Path.GetTempFileName() + ".pdf";
                using (FileStream stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    stream.Write(pdfBytes, 0, pdfBytes.Length);
                }

                ComponentInfo.SetLicense("FREE-LIMITED-KEY");

                using (GemBox.Pdf.PdfDocument _doc = GemBox.Pdf.PdfDocument.Load(tempFilePath))
                {
                    // Print PDF document to default printer (e.g. 'Microsoft Print to Pdf').
                    try
                    {
                        //string printerName = null;
                        _doc.Print(_printerName);
                    }
                    catch (Exception ex)
                    {
                        Spooler.Models.Logger.loggerMessege("error en _doc.Print: " + ex.StackTrace);
                    }

                }


            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.StackTrace);
            }


        }

        private void  button1_ClickAsync(object sender, EventArgs e)
        {
            
            Spooler.Parameters.Getvalues();
            LoadDocument(Spooler.Parameters.PrinterName);
            PrintTicket(_document,null);


        }

        private void button2_Click(object sender, EventArgs e)
        {
            Spooler.Parameters.Getvalues();
            LoadDocument(Spooler.Parameters.PrinterName);

            documentBindingSource.DataSource = _document;
            clientBindingSource.DataSource = _document.Client;
            productBindingSource.DataSource = _document.Products;
            noteBindingSource.DataSource = _document.Notes;
            totalBindingSource.DataSource = _document.Totals;
            this.reportViewer1.RefreshReport();

            //string mimeType;
            //string encoding;
            //string fileNameExtension;
            //string[] streamIds;
            //string contentType;
            //string extension;
            //Microsoft.Reporting.WinForms.Warning[] warnings;

            reportViewer1.LocalReport.Refresh();

        }
    }
}
