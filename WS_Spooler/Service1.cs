using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Spooler.Controller;


namespace WS_Spooler
{
    public partial class Service1 : ServiceBase
    {
        Line _line = new Line();

        public Service1()
        {
            InitializeComponent();           
        }

        protected override void OnStart(string[] args)
        {
            //_line = new Line();
            _line.BeginProcess();

        }

        protected override void OnStop()
        {
            _line._stopProcess = true;
        }

    }
}
