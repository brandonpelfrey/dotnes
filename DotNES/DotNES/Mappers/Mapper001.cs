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

        byte CHR0BankSelect = 0;
        byte CHR1BankSelect = 0;
        byte PRGBankSelect = 0;
        byte ControlRegister = 0x10;

        // CPU $8000-$FFFF is connected to a common shift register.
        byte shift_register;
        int shift_register_write_counter = 0;

        public Mapper001(Cartridge cartridge)
        {
            this.cartridge = cartridge;
        }

        public override bool mapsCHR()
        {
            return true;
        }

        private byte PRGRomBankMode()
        {
            return (byte)((ControlRegister >> 2) & 3);
        }

        enum CHRSelectMode
        {
            EightK, FourK
        }

        private CHRSelectMode CHRSelectSize()
        {
            return ((ControlRegister >> 4) & 1) == 0 ? CHRSelectMode.EightK : CHRSelectMode.FourK;
        }

        public override byte read(ushort address)
        {
            if (address < 0x8000)
            {
                int offset = address - 0x6000;
                return PRG_RAM[offset];
            }

            // 32K PRG Rom mode
            if (PRGRomBankMode() < 2)
            {
                int PRG32KBank = 0;
                return cartridge.PRGRomData[PRG32KBank * 0x8000 + (address - 0x8000)];
            }

            // First bank is fixed at 0x8000, switchable at 0xC000
            else if (PRGRomBankMode() == 2)
            {
                if (address < 0xC000)
                {
                    int offset = address - 0x8000;
                    return cartridge.PRGRomData[offset];
                }
                else
                {
                    int offset = address - 0xC000;
                    return cartridge.PRGRomData[0x4000 * PRGBankSelect + offset];
                }
            }

            // Last bank is fixed at 0xC000, switchable at 0x8000
            else
            {
                if (address < 0xC000)
                {
                    int offset = address - 0x8000;
                    return cartridge.PRGRomData[0x4000 * PRGBankSelect + offset];
                }
                else
                {
                    int offset = address - 0xC000;
                    return cartridge.PRGRomData[0x4000 * (cartridge.PRGROM_16KBankCount - 1) + offset];
                }
            }
        }

        public override byte readCHR(ushort address)
        {
            if (CHRSelectSize() == CHRSelectMode.EightK)
            {
                if (address < 0x1000)
                    return cartridge.CHRRomData[CHR0BankSelect * 0x1000 + (address - 0x0000)];
                else
                    return cartridge.CHRRomData[CHR1BankSelect * 0x1000 + (address - 0x1000)];
            }
            else
            {
                if (address < 0x1000)
                    return cartridge.CHRRomData[CHR0BankSelect * 0x2000 + (address - 0x0000)];
                else
                    return cartridge.CHRRomData[CHR0BankSelect * 0x2000 + (address - 0x1000)];
            }
        }

        public override void write(ushort address, byte val)
        {
            // Writing with bit 7 set, this write just resets the shift register
            if ((val & 0x80) != 0)
            {
                shift_register = 0x10;
                shift_register_write_counter = 0;
                ControlRegister |= 0x0C;
                return;
            }

            // We're shifting in a bit to the shift register..
            shift_register = (byte)((shift_register >> 1) | ((val & 1) << 4));
            shift_register_write_counter++;

            // Should we do an actual write?
            if (shift_register_write_counter == 5)
            {
                if (address < 0xA000)
                {
                    ControlRegister = (byte)(shift_register & 1);

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
                    CHR0BankSelect = (byte)(shift_register);
                    Console.WriteLine("CHR Bank 0 := {0}", CHR0BankSelect);
                }
                else if (address < 0xE000)
                {
                    CHR1BankSelect = (byte)(shift_register);
                    Console.WriteLine("CHR Bank 1 := {0}", CHR1BankSelect);
                }
                else
                {
                    PRGBankSelect = (byte)(shift_register);
                    Console.WriteLine("PRG Mode {0}", PRGBankSelect);
                }

                shift_register_write_counter = 0;
                shift_register = 0x10;
            }
        }
    }
}
