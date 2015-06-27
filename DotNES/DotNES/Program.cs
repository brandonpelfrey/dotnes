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
            
            Console.Out.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
