using System;
using System.Collections.Generic;


namespace Spooler.Models
{
    public class BaseDocument
    {
        public String Number { get; set; } = "";
        public String Reference { get; set; }
        public String Printer_Serial { get; set; }
        public String Type { get; set; }
    
    }
}
