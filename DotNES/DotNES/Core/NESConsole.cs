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
        public Cartridge cartridge { get; set; }

        private long _CpuCycle;
        public long CPUCyclesExecuted 
        {
            get {
                return _CpuCycle;
            }
        }

        public NESConsole(Cartridge cartridge)
        {
            this.cartridge = cartridge;
            this.mapper = cartridge.getMapper();
            this.memory = new Memory(this);
            this.cpu = new CPU(this);
            this.ppu = new PPU(this);
            this.apu = new APU();
            this.io = new IO(KeyboardController.DEFAULT_PLAYER_ONE_CONTROLLER, KeyboardController.DEFAULT_PLAYER_TWO_CONTROLLER);
            //this.io = new IO(new FM2TASController("C:\\roms\\philc2-donkeykong.fm2", 1, this), KeyboardController.DEFAULT_PLAYER_TWO_CONTROLLER);
        }

        public void coldBoot()
        {
            _CpuCycle = 0;
            this.cpu.coldBoot();
            this.ppu.coldBoot();
        }

        public void loadRom(string romPath)
        {
            byte[] romData = File.ReadAllBytes(romPath);
        }

        public void step()
        {
            int cpuCycles = cpu.step();
            this._CpuCycle += cpuCycles;
            for (int i = 0; i < cpuCycles; ++i)
                ppu.step();
        }
    }
}
