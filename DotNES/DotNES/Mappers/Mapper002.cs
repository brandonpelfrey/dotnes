using DotNES.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Mappers
{
    [Mapper("UNROM", 2)]
    class Mapper002 : Mapper
    {
        Cartridge cartridge;
        int selectedBank = 0;

        public Mapper002(Cartridge cartridge)
        {
            this.cartridge = cartridge;
        }

        public override bool mapsCHR()
        {
            return false;
        }

        public override byte read(ushort address)
        {
            if(address < 0x8000)
            {
                Console.Out.WriteLine(string.Format("Invalid read to UNROM Mapper @ {0:X4}", address));
                Console.Out.Flush();
                return 0;
                //throw new IndexOutOfRangeException();
            }
            
            if(address >= 0xC000)
                return cartridge.PRGRomData[0x4000 * (cartridge.PRGROM_16KBankCount - 1 ) + address - 0xC000];
            else
                return cartridge.PRGRomData[0x4000 * (selectedBank) + address - 0x8000];
        }

        public override byte readCHR(ushort address)
        {
            throw new NotImplementedException();
        }

        public override void write(ushort address, byte val)
        {
            selectedBank = val & 0x0F;
        }
    }
}
