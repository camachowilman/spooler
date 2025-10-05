using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooler.Models
{
    public static class Logger
    {

        public static string logfile = "";

        public static void loggerMessege(string _messege)
        {
            if (Spooler.Parameters.Logger)
                Console.WriteLine(_messege);
            RegistrarExcepcion(_messege);
        }

        public static void loggerMessege(Exception ex)
        {
            if (Spooler.Parameters.Logger)
                Console.WriteLine(ex.Message+ " StackTrace: " + ex.StackTrace);
            RegistrarExcepcion(ex.Message + " StackTrace: " + ex.StackTrace);
        }


        private static void CrearDirectorio()
        {
            if (!Directory.Exists(Spooler.Parameters.Logpath))
            {
                Directory.CreateDirectory(Spooler.Parameters.Logpath);
            }
        }
        private static void CrearArchivo()
        {
            logfile = Spooler.Parameters.Logpath + "/Spooler.txt";

            if (!File.Exists(logfile))
            {
                FileStream archivo;
                archivo = File.Create(logfile);
                archivo.Close();
            }
        }
        private static void GestionarDirectorio()
        {
            
            CrearDirectorio();
            CrearArchivo();

            FileInfo fInfo = new FileInfo(logfile);
            if (fInfo.Length > 10485760)
            {
                fInfo.MoveTo(logfile + "_" + DateTime.Now.Day.ToString("D2") +
                                                DateTime.Now.Month.ToString("D2") +
                                                DateTime.Now.Year.ToString("D4") +
                                                DateTime.Now.Hour.ToString("D2") +
                                                DateTime.Now.Minute.ToString("D2") +
                                                DateTime.Now.Second.ToString("D2"));
                CrearArchivo();
            }

        }

        public static void RegistrarExcepcion(string sMensaje)
        {
            try
            {
                GestionarDirectorio();

                string str;

                using (StreamReader sreader = new StreamReader(logfile))
                {
                    str = sreader.ReadToEnd();
                }

                using (StreamWriter writer = new StreamWriter(logfile, false))
                {
                    writer.WriteLine("Fecha     = " + DateTime.Now);
                    writer.WriteLine("ERROR     = " + sMensaje);
                    writer.WriteLine("===========================================================================================================================");
                    writer.WriteLine(str);
                }
            }
            catch
            {
            }

        }


    }
}
