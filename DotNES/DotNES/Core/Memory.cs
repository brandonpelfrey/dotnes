using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    public class Memory
    {
        private Cartridge cartridge;

        public Memory(Cartridge cartridge)
        {
            this.cartridge = cartridge;
        }

        /// <summary>
        /// Write a byte to system memory. This might be internal RAM ($0000-$07FF), or 
        /// could refer to memory-mapped devices, e.g. mappers, APU, PPU, etc.
        /// </summary>
        /// <param name="addr">The 16-bit address at which to set a value.</param>
        /// <param name="val">The byte to write at the given location.</param>
        public void write8(ushort addr, byte val)
        {
            throw new NotImplementedException();
        }

        public byte read8(ushort addr)
        {
            throw new NotImplementedException();
        }

        public ushort read16(ushort addr)
        {
            throw new NotImplementedException();
        }
    }
}
