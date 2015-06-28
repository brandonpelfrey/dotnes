using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Mappers
{
    public abstract class Mapper
    {
        public abstract byte read(ushort address);

        public abstract void write(ushort address, byte val);
    }
}
