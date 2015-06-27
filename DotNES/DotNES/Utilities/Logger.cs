using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Utilities
{
    class Logger
    {
        // TODO : add class name to the log. Also, per-class logging on/off functionality
        //private string className;

        public Logger()
        {
        }

        public void info(string message, params object[] args)
        {
            Console.Out.WriteLine(string.Format(message, args));
        }

        public void error(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}
