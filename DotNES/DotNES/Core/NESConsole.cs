using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    class NESConsole
    {
        private Cartridge cartridge;
        private Memory memory;

        private CPU cpu;
        private PPU ppu;
        private APU apu;

        public NESConsole(Cartridge cartridge)
        {
            this.cartridge = cartridge;
            this.memory = new Memory(cartridge);

            this.cpu = new CPU( this.memory );
            this.ppu = new PPU();
            this.apu = new APU();
        }

        public void loadRom(string romPath)
        {
            byte[] romData = File.ReadAllBytes(romPath);
        }
        
        private void step()
        {
            int cpuCycles = cpu.step();
        }
    }
}
