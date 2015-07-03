using DotNES.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NESEmulator emulator = new NESEmulator())
            {
                emulator.Run();
            }
        }
    }
}
