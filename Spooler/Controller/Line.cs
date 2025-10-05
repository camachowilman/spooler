using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using RestSharp;
using Spooler;
using Spooler.HKA;
using Spooler.Models;
using Spooler.Ticket;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.Net.Http;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Web;

namespace Spooler.Controller
{
    public class Line
    {

        public List<Record> _Line { get; set; } = new List<Record>();
        public NpgsqlConnection _conection = new NpgsqlConnection();
        public bool _stopProcess = false;
        Task _taskBeginProcess;
        public string _printerSerial = string.Empty;
        public string _printerName = string.Empty;

        public Line()
        {

            Spooler.Parameters.Getvalues();
            Spooler.HKA.Printer.SetPort(Spooler.Parameters.Port);

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

        private string GetPrinterSerial()
        {
        inicio:
            try
            {
                string _serial = "";
               // _serial = Spooler.HKA.Printer.GetSerialNumber();  // habilitar 

                _serial = Spooler.Parameters.PrinterName; // quitar

                if (_serial != "")
                    return _serial;
                else
                {
                    Spooler.Models.Logger.loggerMessege("Error: Impresora Fiscal no instalada");
                    System.Threading.Thread.Sleep(1000);
                    goto inicio;
                }

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                System.Threading.Thread.Sleep(1000);
                goto inicio;
            }
        }

        private string GetPrinterName()
        {
        inicio:
            try
            {
                string _serial = "";
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (printer.Contains(Spooler.Parameters.PrinterName))
                    {
                        _serial = printer;
                        break;
                    }
                }


                if (_serial != "")
                    return _serial;
                else
                {
                    Spooler.Models.Logger.loggerMessege("Error: Impresora tickera no instalada");
                    System.Threading.Thread.Sleep(1000);
                    goto inicio;
                }

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                System.Threading.Thread.Sleep(1000);
                goto inicio;
            }
        }

        public void LoadRecordsPOSTGRES(string _serialCode)
        {

            try
            {

                if (Connect())
                {
                    NpgsqlCommand _cmd = new NpgsqlCommand();
                    _cmd.CommandText = "select * from getline(:v_printer_serial)";
                    _cmd.Parameters.AddWithValue("@v_printer_serial", NpgsqlTypes.NpgsqlDbType.Varchar, _serialCode);
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

                        if (!_Line.Exists(x => x.Id == _record.Id))
                            _Line.Add(_record);

                    }
                    _conection.Close();
                }
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }

        }

        public void LoadRecords(string _serialCode)
        {

            try
            {

                using (SqlConnection connection = new SqlConnection(Spooler.Parameters.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand comando = new SqlCommand("USP_CON_COLAIMPRESION"))
                    {
                        comando.Connection = connection;
                        comando.CommandType = CommandType.StoredProcedure;
                        comando.Parameters.Add(new SqlParameter("@VSERIAL", _serialCode));
 

                        using (SqlDataReader dr = comando.ExecuteReader())
                        {
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

                                if (!_Line.Exists(x => x.Id == _record.Id))
                                    _Line.Add(_record);

                            }
                            dr.Close();
                        }
                       
                    }
                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }

        }

        private void IterateRecords()
        {

            foreach (Record _record in _Line)
            {

                _record.Printed = false;
                _record.DocumentNumber = "";

                if (_record.Type == typeDocument.Invoice)
                {

                    if (!_record.Reprint)
                        {
                        Record _lastInvoice = GetLastDocumentPrintedInSpooler(_printerSerial, "F");
                        _record.PrintInvoice(_lastInvoice);
                        }                       
                    else
                        _record.RePrintInvoice();
                }

                if (_record.Type == typeDocument.Refund)
                {
                    if (!_record.Reprint)
                        _record.PrintRefund();
                    else
                        _record.RePrintRefund();
                }

                if (_record.Type == typeDocument.XReport)
                {
                    _record.PrintXReport();
                }

                if (_record.Type == typeDocument.ZReport)
                {
                    if (!_record.Reprint)
                        _record.PrintZReport();
                    else
                        _record.RePrintZReport();
                }

                if (_record.Type == typeDocument.PayMethods)
                {
                    _record.PrintPayMethods();
                }

                if (_record.Type == typeDocument.Ticket)
                {
                    _record.PrintTicket(_printerName);
                }


                if (_record.Printed)
                    UpdateRecordFromAPI(_record.Id, _record.DocumentNumber, "");
                //UpdateRecord(_record.Id, _record.DocumentNumber, "");


                System.Threading.Thread.Sleep(500);
            }
        }

        public void BeginProcess()
        {
        inicio:
            try
            {
               //_taskBeginProcess = new Task(_Process);
               //_taskBeginProcess.Start();

                _Process();
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                System.Threading.Thread.Sleep(2000);
                goto inicio;
            }
        }

        public void _Process()
        {
        inicio:
            try
            {
                System.Threading.Thread.Sleep(5000);
                Spooler.Models.Logger.loggerMessege("Servicio Iniciado!...");

                
                if (Spooler.Parameters.Port != "")
                    _printerSerial = GetPrinterSerial();
                else
                {
                    Spooler.Models.Logger.loggerMessege("Advertencia: Puerto para Impresora fiscal no definida");

                    if (Spooler.Parameters.PrinterName != "")
                        _printerName = GetPrinterName();
                    else
                        Spooler.Models.Logger.loggerMessege("Advertencia: Impresora Tickera no definida");
                }
                    

                
                while (!_stopProcess)
                {
                    _Line.Clear();
                    if (_printerSerial != "")
                        LoadRecords(_printerSerial); // LoadRecordsFromAPI(_printerSerial);

                    if (_printerName != "")
                        LoadRecords(_printerName); // LoadRecordsFromAPI(_printerName);

                    IterateRecords();
                    System.Threading.Thread.Sleep(1000);
                }
                
                Spooler.Models.Logger.loggerMessege("Servicio Detenido!...");
                System.Threading.Thread.Sleep(5000);

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                System.Threading.Thread.Sleep(2000);
                goto inicio;
            }

        }

        private void UpdateRecord(int id, string numberDocument, string printedNote)
        {
            try
            {
                if (Connect())
                {
                    NpgsqlCommand _cmd = new NpgsqlCommand();
                    _cmd.CommandText = "select updateline(:id,:numberdocument,:printednote)";
                    _cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Integer, id);
                    _cmd.Parameters.AddWithValue("@numberdocument", NpgsqlTypes.NpgsqlDbType.Varchar, numberDocument);
                    _cmd.Parameters.AddWithValue("@printednote", NpgsqlTypes.NpgsqlDbType.Varchar, printedNote);
                    _cmd.CommandType = CommandType.Text;
                    _cmd.Connection = _conection;
                    _cmd.ExecuteNonQuery();
                    _conection.Close();
                }
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }

        }

        private void ProbarConversionJSON()
        {

            foreach (Record _record in _Line)
            {

                if (_record.Type == typeDocument.Invoice)
                {
                    if (!_record.Reprint)
                        _record.ConvertJasonValueToDocument();
                    else
                        _record.ConvertJasonValueToBaseDocument();
                }

                if (_record.Type == typeDocument.Refund)
                {
                    if (!_record.Reprint)
                        _record.ConvertJasonValueToDocument();
                }

                if (_record.Type == typeDocument.XReport)
                {
                    _record.ConvertJasonValueToBaseDocument();
                }

                if (_record.Type == typeDocument.ZReport)
                {
                    if (!_record.Reprint)
                        _record.ConvertJasonValueToBaseDocument();
                    else
                        _record.ConvertJasonValueToBaseDocument();
                }

                System.Threading.Thread.Sleep(500);
            }
        }

        private dynamic PostAPI(string _url, string _json)
        {
            try
            {
                var _client = new RestClient(_url);
                var _request = new RestRequest();
                _request.Method = Method.Post;
                _request.AddHeader("Content-Type", "application/json");
                _request.AddParameter("application/json", _json, ParameterType.RequestBody);

                RestResponse _response = _client.Execute(_request);

                if (_response.ResponseStatus == ResponseStatus.Error)
                {
                    throw new Exception(_response.ErrorException.InnerException.Message + " : " + _url);
                }

                dynamic _datos = JsonConvert.DeserializeObject(_response.Content);
                return _datos;

            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
                return false;
            }
        }
        private void LoadRecordsFromAPI(string _serialCode)
        {
            try
            {

                var printerSerialObj = new { printer_serial = _serialCode };
                string _json = JsonConvert.SerializeObject(printerSerialObj);
                dynamic _result = PostAPI(Spooler.Parameters.Url + "/get_line", _json);

                if (_result.ToString() != "False")
                {


                    JArray jsonArray = JArray.Parse(_result.ToString());
                    foreach (JToken item in jsonArray)
                    {
                        JObject currentItem = item as JObject;
                        Record _record = new Record();
                        _record.Id = (int)currentItem["id"];
                        _record.JasonValue = currentItem["document_json"].ToString();
                        _record.Reference = currentItem["document_reference"].ToString();
                        _record.Printer_Serial = currentItem["printer_serial"].ToString();
                        _record.Type = (typeDocument)(int)(currentItem["document_type"]);
                        _record.Reprint = currentItem["reprint"].ToObject<bool>();
                        _record.Document = new Document();
                        _record.BaseDocument = new BaseDocument();

                        if (!_Line.Exists(x => x.Id == _record.Id))
                            _Line.Add(_record);
                    }

                }

            }
            catch (HttpRequestException ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
        }

        private void UpdateRecordFromAPI(int _id, string _numberDocument, string printedNote)
        {
            try
            {

                var _obj = new { id = _id, document_number = _numberDocument };
                string _json = JsonConvert.SerializeObject(_obj);
                dynamic _result = PostAPI(Spooler.Parameters.Url + "/update_line", _json);

            }
            catch (HttpRequestException ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
        }

        private Record  GetLastDocumentPrintedInSpooler(string _serialCode, string _document_type )
        {
            Record _record = new Record();

            try
            {

                using (SqlConnection connection = new SqlConnection(Spooler.Parameters.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand comando = new SqlCommand("USP_CON_ULTIMAIMPRESION"))
                    {
                        comando.Connection = connection;
                        comando.CommandType = CommandType.StoredProcedure;
                        comando.Parameters.Add(new SqlParameter("@VSERIAL", _serialCode));
                        comando.Parameters.Add(new SqlParameter("@VTIPO", _document_type));

                        using (SqlDataReader dr = comando.ExecuteReader())
                        {
                            while (dr.Read())
                            {

                                _record.Id = dr.GetInt32(dr.GetOrdinal("id"));
                                _record.Reference = dr.GetString(dr.GetOrdinal("document_reference"));
                                _record.DocumentNumber = dr.GetString(dr.GetOrdinal("document_number"));

                            }
                            dr.Close();
                        }

                    }
                    connection.Close();
                }

            }
            catch (HttpRequestException ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }
            catch (Exception ex)
            {
                Spooler.Models.Logger.loggerMessege(ex.Message);
            }

            return _record;
        }




    }
}
