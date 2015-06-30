using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    public class APU
    {
        public void step()
        {

        }

        public byte read(ushort address)
        {
            // TODO: APU register reads
            return 0;
        }

        public void write(ushort addr, byte val)
        {
            // throw new NotImplementedException();
        }
    }
}
