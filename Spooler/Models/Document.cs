using System;
using System.Collections.Generic;


namespace Spooler.Models
{
    public class Document
    {
        public String BillNumber { get; set; } = "";
        public String BillDate { get; set; } = string.Empty;
        public String Reference { get; set; }
        public String Printer_Serial { get; set; }
        public String Type { get; set; }
        public Client Client { get; set; } = new Client();
        public List<Product> Products { get; set; }= new List<Product>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public List<Total> Totals { get; set; } = new List<Total>();
        public List<Note> Notes { get; set; } = new List<Note>();
        public decimal Discount { get; set; }
        public String Text { get; set; } = "";


    }
}
