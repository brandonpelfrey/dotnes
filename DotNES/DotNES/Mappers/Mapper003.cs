using DotNES.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Mappers
{
    [Mapper("CNROM", 3)]
    class Mapper003 : Mapper
    {
        Cartridge cartridge;
        int selectedBank = 0;

        public Mapper003(Cartridge cartridge)
        {
            this.cartridge = cartridge;
        }

        public override bool mapsCHR()
        {
            return true;
        }

        public override byte read(ushort address)
        {
            if (address < 0x8000)
            {
                Console.Error.WriteLine(string.Format("Invalid read to CNROM Mapper @ {0:X4}", address));
                return 0;
            }

            if (cartridge.PRGROM_16KBankCount == 1 && address >= 0xC000)
                address -= 0x4000;

            return cartridge.PRGRomData[address - 0x8000];
        }

        public override byte readCHR(ushort address)
        {
            return cartridge.CHRRomData[0x2000 * selectedBank + address];
        }

        public override void write(ushort address, byte chrBankSelect)
        {
            selectedBank = chrBankSelect & 3;
            Console.WriteLine("bank {0:X4} <- {1}", address, chrBankSelect);
        }
    }
}
