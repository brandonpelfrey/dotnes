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

        // Returns true/false depending on if the mapper maps VRAM 0x0000-0x2000 to CHR-ROM
        public abstract bool mapsCHR();

        // Read CHR-ROM (possible bank-switched) through the mapper
        public abstract byte readCHR(ushort address);
    }
}
