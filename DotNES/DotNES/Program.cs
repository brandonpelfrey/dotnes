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
            Cartridge cart = new Cartridge("C:\\roms\\dk.nes");
            NESConsole system = new NESConsole(cart);
            system.cpu.coldBoot();

            system.ppu.setLoggerEnabled(false);
            system.cpu.setLoggerEnabled(false);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            long instructionsToRun = 17 * 1000000;
            for (int i = 0; i < instructionsToRun; ++i)
            {
                system.step();

                if (false)
                {
                    Console.ReadKey();
                }
            }

            stopwatch.Stop();
            long elapsedMillis = stopwatch.ElapsedMilliseconds;
            float emulationRate = (float)system.CPUCyclesExecuted / (float)elapsedMillis * 1000f;

            Console.Out.WriteLine(string.Format("Executed {0} CPU Instructions", instructionsToRun));
            Console.Out.WriteLine(string.Format("Executed {0} CPU cycles in {1} ms ({2} cycles/sec)", system.CPUCyclesExecuted, elapsedMillis, emulationRate));
            Console.Out.WriteLine(string.Format("Finished {0} PPU Frame", system.ppu.FrameCount));
            Console.Out.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
