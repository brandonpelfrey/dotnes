using DotNES.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    /// <summary>
    /// http://wiki.nesdev.com/w/index.php/PPU_rendering
    /// Information on palettes: http://wiki.nesdev.com/w/index.php/PPU_palettes
    /// </summary>
    public class PPU
    {
        private Logger log = new Logger("PPU");
        public void setLoggerEnabled(bool enable)
        {
            this.log.setEnabled(enable);
        }

        private NESConsole console;

        private bool nmiOccurred = false;
        private bool oddFrame = false;

        // Current column and scanline (int to support pre-scanline convention -1, etc.)
        private int x, scanline;

        #region Registers

        byte _PPUCTRL;
        byte _PPUMASK;
        byte _PPUSTATUS;

        byte _PPUSCROLL;
        byte _PPUADDR;
        byte _PPUDATA;

        // Object Attribute Memory (OAM) is memory internal to the PPU that contains sprite 
        // data (positions, mirroring, priority, pallete, etc.) for up to 64 sprites, each taking up 4 bytes.
        byte _OAMDATA;
        byte _OAMADDR;
        byte _OAMDMA;

        #endregion

        public PPU(NESConsole console)
        {
            this.console = console;
        }

        public void render_step()
        {
            // The first pre-scanline (-1) alternates every scanline between 340 and 341 pixels.
            // For all other scanlines, the scanline is 341 pixels [0,340].
            // Actual visible pixels are [0,256), with [256,340] dedicated to preparing for sprites 
            // in the following scanline.

            // There are 262 total scanlines per frame:
            // -1      : Pre-scanline        0-239   : Visible scanlines
            // 240     : Nada?               241-260 : Vertical Blanking

            // At the very start of the VBlank region, NMI is triggered.
            if (scanline >= 241 && x == 1)
            {
                _PPUSTATUS |= 0x80;

                // Potentially trigger NMI
                if ((byte)(_PPUCTRL & 0x80) != 0 && !nmiOccurred)
                {
                    nmiOccurred = true;
                    console.cpu.nmi = true;
                }
            }

            x++;

            // Variable line width for the pre-scanline depending on even/odd frame
            if (scanline == -1 && oddFrame && x == 339)
            {
                x = 0;
                scanline++;
            }
            else if (x == 340)
            {
                scanline++;
                x = 0;
            }

            if (scanline == 261)
            {
                scanline = -1;
                nmiOccurred = false;
            }

            log.info("{0},{1}", x, scanline);
        }

        public void step()
        {
            render_step();
        }

        public void coldBoot()
        {
            _PPUCTRL = _PPUMASK = _PPUSTATUS = 0;
            _OAMADDR = 0;
            _PPUSCROLL = _PPUADDR = _PPUDATA = 0;
            oddFrame = false;
        }

        public void warmBoot()
        {
            // AFAIK These are effectively the same except for some extremely exceptional cases
            coldBoot();
        }

        public byte read(ushort addr)
        {
            if (addr == 0x2002)
            {
                // Reading the status register clears the "NMI Occurred" flag, but still returns
                // the old value of the register before clearing the flag.
                byte result = _PPUSTATUS;
                //byte nmiBit = (byte)(nmiOccurred ? 0x80 : 0);
                //result = (byte)((result & 0x7F) | (nmiBit));
                nmiOccurred = false;

                _PPUSTATUS &= 0x7F;
                return result;
            }
            log.error("Unimplemented read to PPU @ {0:X}", addr);
            return 0;
        }

        public void write(ushort addr, byte val)
        {
            if (addr == 0x2000)
            {
                _PPUCTRL = val;
                log.info("CTRL : {0:X2}", _PPUCTRL);
            }
            else
            {
                log.error("Unimplemented write 0x{0:X2} to PPU @ {1:X4}", val, addr);
            }
        }
    }
}
