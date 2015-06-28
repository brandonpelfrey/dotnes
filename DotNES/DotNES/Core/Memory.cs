using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    public class Memory
    {
        private byte[] RAM;
        private NESConsole console;

        public Memory(NESConsole console)
        {
            this.console = console;
            RAM = new byte[0x800];
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
            if (addr < 0x2000)
            {
                return RAM[addr & 0x77F];
            }
            else if (addr < 0x4000)
            {
                // TODO : Read form PPU
                // 0x2000 - 0x2007 repeats every 8 bytes up until 0x3FFF
            }
            else if (addr < 0x4016)
            {
                return console.apu.read(addr);
            }
            else if (addr < 0x4018)
            {
                return console.io.read(addr);
            }
            else
            {
                return console.mapper.read(addr);
            }

            throw new NotImplementedException();
        }

        public void write16(ushort addr, ushort val)
        {
            throw new NotImplementedException();
        }

        public ushort read16(ushort addr)
        {
            return (ushort)((read8((ushort)(addr + 1)) << 8) | read8(addr));
        }
    }
}
