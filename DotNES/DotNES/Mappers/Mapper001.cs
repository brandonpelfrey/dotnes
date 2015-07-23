using DotNES.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Mappers
{
    [Mapper("MMC1", 1)]
    class Mapper001 : Mapper
    {
        Cartridge cartridge;

        // CPU $6000-$7FFF: 8 KB PRG RAM bank, fixed on all boards but SOROM and SXROM
        byte[] PRG_RAM = new byte[0x2000];

        byte ControlRegister = 0x1C;

        int PRGSelect = 0;
        int CHR0Select = 0;
        int CHR1Select = 0;

        int[] PRGOffsets = { 0, 0 };
        int[] CHROffsets = { 0, 0 };

        // CPU $8000-$FFFF is connected to a common shift register.
        byte shift_register = 0x00;
        int shift_register_write_counter = 0;

        public Mapper001(Cartridge cartridge)
        {
            this.cartridge = cartridge;

            PRGOffsets[0] = 0;
            PRGOffsets[1] = (cartridge.PRGROM_16KBankCount - 1) * 0x4000;
        }

        public override bool mapsCHR()
        {
            return true;
        }

        public override byte read(ushort address)
        {
            if (address < 0x8000)
            {
                int offset = address - 0x6000;
                return PRG_RAM[offset];
            }

            if (address >= 0xC000)
                return cartridge.PRGRomData[PRGOffsets[1] + (address - 0xC000)];
            else
                return cartridge.PRGRomData[PRGOffsets[0] + (address - 0x8000)];
        }

        public override byte readCHR(ushort address)
        {
            if (address < 0x1000)
                return cartridge.CHRRomData[CHROffsets[0] + (address - 0x0000)];
            else
                return cartridge.CHRRomData[CHROffsets[1] + (address - 0x1000)];
        }

        private void updateOffsets()
        {
            // CHR0 and CHR1
            if ((ControlRegister & 0x10) == 0)
            {
                CHROffsets[0] = 0x2000 * CHR0Select;
                CHROffsets[1] = 0x2000 * CHR0Select + 0x1000;
            }
            else
            {
                CHROffsets[0] = 0x1000 * CHR0Select;
                CHROffsets[1] = 0x1000 * CHR1Select;
            }

            // PRG Select
            if ((ControlRegister & 0x08) == 0)
            {
                Console.WriteLine("A");
                PRGOffsets[0] = 0x4000 * (PRGSelect & 0xFE);
                PRGOffsets[1] = 0x4000 * (PRGSelect | 0x01);
            }
            else
            {
                // 16KB PRG Switching. There are two possible modes for switching.

                // If Control's Bit 2 is set, PRG is swapped at 0xC0000
                if ((ControlRegister & 0x04) == 0)
                {
                    Console.WriteLine("B");
                    PRGOffsets[0] = 0;
                    PRGOffsets[1] = 0x4000 * PRGSelect;
                }
                else
                {
                    Console.WriteLine("C");
                    PRGOffsets[0] = 0x4000 * PRGSelect;
                    PRGOffsets[1] = 0x4000 * (cartridge.PRGROM_16KBankCount - 1);
                }

            }

        }

        public override void write(ushort address, byte val)
        {
            if(address < 0x6000)
            {
                Console.WriteLine("Wat. {0:X4} {1:X2}", address, val);
                return;
            }

            // First handle RAM. Everything else uses the shift register
            if (address < 0x8000)
            {
                int offset = address - 0x6000;
                PRG_RAM[offset] = val;
            }
            

            // Writing with bit 7 set, this write just resets the shift register
            if ((val & 0x80) != 0)
            {
                shift_register = 0x00;
                ControlRegister |= 0x0C;
                shift_register_write_counter = 0;

                //updateOffsets();
                return;
            }

            Console.WriteLine("WRITE)");

            // We're shifting in a bit to the shift register..
            shift_register >>= 1;
            shift_register |= (byte)((val & 1) << 4);
            shift_register_write_counter++;

            // Should we do an actual write?
            if(shift_register_write_counter == 5)
            {
                if (address < 0xA000)
                {
                    ControlRegister = shift_register;

                    switch (ControlRegister & 3)
                    {
                        case 0: cartridge.NametableMirroring = NametableMirroringMode.OneScreenLowBank; break;
                        case 1: cartridge.NametableMirroring = NametableMirroringMode.OneScreenHighBank; break;
                        case 2: cartridge.NametableMirroring = NametableMirroringMode.Vertical; break;
                        case 3: cartridge.NametableMirroring = NametableMirroringMode.Horizontal; break;
                    }

                    Console.WriteLine("Mirroring set to {0}", cartridge.NametableMirroring.ToString());
                }
                else if (address < 0xC000)
                {
                    CHR0Select = shift_register;
                }
                else if (address < 0xE000)
                {
                    CHR1Select = shift_register;
                }
                else
                {
                    PRGSelect = shift_register & 0x0F;
                }

                // Update offsets
                updateOffsets();

                shift_register = 0x00;
                shift_register_write_counter = 0;
            }
        }
    }
}
