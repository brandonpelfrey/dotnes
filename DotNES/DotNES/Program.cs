using DotNES.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES
{
    class Program
    {
        static void Main(string[] args)
        {
            Cartridge cart = new Cartridge("C:\\roms\\dk.nes");
            NESConsole system = new NESConsole( cart );
            system.cpu.coldBoot();

            system.ppu.setLoggerEnabled(false);
            //system.cpu.setLoggerEnabled(false);
            
            for (int i = 0; i < 1000000; ++i)
            {
                system.step();
                
                if(false)
                {
                    Console.ReadKey();
                }
            }

            Console.Out.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
