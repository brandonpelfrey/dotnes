using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Utilities
{
    class Logger
    {
        // Would be really nice to just pass the class type itself to the constructor. I'll figure it out later.
        private string className = "";

        public Logger(string className)
        {
            this.className = className;
        }

        public void info(string message, params object[] args)
        {
            Console.Write("{0,-15} ", className);
            Console.Out.WriteLine(string.Format(message, args));
        }

        public void error(string message, params object[] args)
        {
            Console.Write("{0,-15} ", className);
            Console.Error.WriteLine(string.Format(message, args));
        }
    }
}
