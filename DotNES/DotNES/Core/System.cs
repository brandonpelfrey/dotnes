using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    class System
    {
        Memory memory;
        CPU cpu;

        public void loadRom(string romPath)
        {
            byte[] romData = File.ReadAllBytes(romPath);
        }
    }
}
