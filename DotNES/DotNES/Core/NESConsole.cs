using DotNES.Mappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    public class NESConsole
    {
        public Mapper mapper;
        public Memory memory { get; set; }
        public CPU cpu { get; set; }
        public PPU ppu { get; set; }
        public APU apu { get; set; }
        public IO io { get; set; }

        public NESConsole(Cartridge cartridge)
        {
            this.mapper = cartridge.getMapper();
            this.memory = new Memory(this);
            this.cpu = new CPU( this );
            this.ppu = new PPU();
            this.apu = new APU();
            this.io = new IO();
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
