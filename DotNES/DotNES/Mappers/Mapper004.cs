using DotNES.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Mappers
{
    [Mapper("MMC3", 4)]
    class Mapper004 : Mapper
    {
        byte IRQCounter = 0;
        byte IRQCounterLatch = 0;
        bool IRQEnable = false;

        bool PRGRAMProtect = false;

        NESConsole console;

        public Mapper004(NESConsole console)
        {
            this.console = console;
        }

        public override bool mapsCHR()
        {
            return true;
        }

        public override byte read(ushort address)
        {
            throw new NotImplementedException();
        }

        public override byte readCHR(ushort address)
        {
            throw new NotImplementedException();
        }

        public override void write(ushort address, byte val)
        {
            throw new NotImplementedException();
        }
    }
}
