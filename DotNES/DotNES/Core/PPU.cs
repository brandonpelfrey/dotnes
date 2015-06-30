using DotNES.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    /// <summary>
    /// http://wiki.nesdev.com/w/index.php/PPU_rendering
    /// </summary>
    public class PPU
    {
        private Logger log = new Logger("PPU");

        public void step()
        {

        }

        public byte read(ushort addr)
        {
            log.error("Unimplemented read to PPU @ {0:X}", addr);
            return 0;
        }

        public void write(ushort addr, byte val)
        {
            log.error("Unimplemented write 0x{0:X2} to PPU @ {1:X4}", val, addr);
        }
    }
}
