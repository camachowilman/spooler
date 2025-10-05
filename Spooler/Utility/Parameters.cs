using Newtonsoft.Json;
using Spooler.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace Spooler
{
    public static class Parameters
    {
       
        public static bool Igtf = false;
        public static string Port = "";
        public static string PrinterName = "";
        public static string ConnectionString = "";
        public static bool Logger = false;
        public static string Logpath = "";
        public static string Url = "";

        private static string ObtenerJSON_test()
        {
            Document _document = new Document();

            _document.Client = new Client();
            _document.Client.Vat = "V16442063";
            _document.Client.Name = "Wilman Camacho";
            _document.Client.Address = "Calle 6 Santa Isabel";

            _document.Products = new List<Product>
            {
                new Product { Code = "001", Name = "Acetaminofen", Quantity = 2, Tax = "16.0",Price = Convert.ToDecimal(64.5) },
                new Product { Code = "002", Name = "Diclofenac", Quantity = 1, Tax = "16.0" , Price = Convert.ToDecimal(70.3) },
                new Product { Code = "003", Name = "Brugesic", Quantity = 1, Tax = "0" , Price = Convert.ToDecimal(40.2) }

            };

            _document.Payments = new List<Payment>
            {
                new Payment { Code = "1", Name = "Efectivo", Amount = 10 },
                new Payment { Code = "2", Name = "Punto", Amount = 25 }

            };

           // _document.Note = "";

            string jfactura = JsonConvert.SerializeObject(_document, Formatting.Indented);

            File.WriteAllText(@"C:\Proyecto\Spooler\SpoolerLib\Json\factura.json", jfactura);

            return jfactura;

        }

        public static void Getvalues()
        {

        inicio:

            try
            {
                if (ConfigurationManager.AppSettings["Igtf"].ToString() == "1")
                    Igtf = true;
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Igtf no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }

            try
            {
                if (ConfigurationManager.AppSettings["Logger"].ToString() == "1")
                    Logger = true;
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Logger no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }

            try
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["datos"].ConnectionString;
                if (ConnectionString == "")
                    {
                    Console.WriteLine("Error: ConnectionString no definido en app.config");
                    System.Threading.Thread.Sleep(5000);
                    goto inicio;
                    }
            }
            catch (Exception)
            {
                
                Console.WriteLine("Error: ConnectionString no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }

            try
            {
                Port = ConfigurationManager.AppSettings["Puerto"].ToString();
                
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Port no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }

            try
            {
                PrinterName = ConfigurationManager.AppSettings["PrinterName"].ToString();

            }
            catch (Exception)
            {
                Console.WriteLine("Error: PrinterName no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }

            try
            {
                Logpath = ConfigurationManager.AppSettings["Logpath"].ToString();
                if (Logpath == "")
                {
                    Console.WriteLine("Error: Logpath no definido en app.config");
                    System.Threading.Thread.Sleep(5000);
                    goto inicio;
                }
            }
            catch (Exception)
            {

                Console.WriteLine("Error: Logpath no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }

            try
            {
                Url = ConfigurationManager.AppSettings["Url"].ToString();
                if (Url == "")
                {
                    Console.WriteLine("Error: Url no definido en app.config");
                    System.Threading.Thread.Sleep(5000);
                    goto inicio;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Url no definido en app.config");
                System.Threading.Thread.Sleep(5000);
                goto inicio;
            }


        }


    }
}
