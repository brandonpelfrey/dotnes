using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    public class IO
    {
        void write(ushort address, byte val)
        {

        }

        public byte read(ushort address)
        {
            // TODO: handle serial controller data
            // http://wiki.nesdev.com/w/index.php/Standard_controller
            return 0;
        }
    }
}
