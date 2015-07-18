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

        private long _frameCount = 0;
        public long FrameCount
        {
            get
            {
                return _frameCount;
            }
        }

        private NESConsole console;

        private bool oddFrame = false;

        // We are not yet at a point where care to emulate the NTSC palette generation
        // If we get to that point, look at the generators/documentation 
        // on http://wiki.nesdev.com/w/index.php/PPU_palettes . For now, this is a
        // pre-computed version found on another wiki online.
        private uint[] RGBA_PALETTE = {
            0x7C7C7CFF,0x0000FCFF,0x0000BCFF,0x4428BCFF,
            0x940084FF,0xA80020FF,0xA81000FF,0x881400FF,
            0x503000FF,0x007800FF,0x006800FF,0x005800FF,
            0x004058FF,0x000000FF,0x000000FF,0x000000FF,
            0xBCBCBCFF,0x0078F8FF,0x0058F8FF,0x6844FCFF,
            0xD800CCFF,0xE40058FF,0xF83800FF,0xE45C10FF,
            0xAC7C00FF,0x00B800FF,0x00A800FF,0x00A844FF,
            0x008888FF,0x000000FF,0x000000FF,0x000000FF,
            0xF8F8F8FF,0x3CBCFCFF,0x6888FCFF,0x9878F8FF,
            0xF878F8FF,0xF85898FF,0xF87858FF,0xFCA044FF,
            0xF8B800FF,0xB8F818FF,0x58D854FF,0x58F898FF,
            0x00E8D8FF,0x787878FF,0x000000FF,0x000000FF,
            0xFCFCFCFF,0xA4E4FCFF,0xB8B8F8FF,0xD8B8F8FF,
            0xF8B8F8FF,0xF8A4C0FF,0xF0D0B0FF,0xFCE0A8FF,
            0xF8D878FF,0xD8F878FF,0xB8F8B8FF,0xB8F8D8FF,
            0x00FCFCFF,0xF8D8F8FF,0x000000FF,0x000000FF
        };

        private uint[] _imageData = new uint[256 * 240];
        public uint[] ImageData
        {
            get
            {
                return _imageData;
            }
        }

        private void drawBackground(int px, int py)
        {
            if ((_PPUMASK & 2) == 0 && px < 8) return;

            int ppu_scroll_x = (_PPUSCROLL >> 8) & 0xFF;
            int ppu_scroll_y = _PPUSCROLL & 0xFF;

            // The two LSBs of PPUCTRL hold the MSB of the ppu scroll offsets
            ppu_scroll_x += ((_PPUCTRL >> 0) & 1) * 256;
            ppu_scroll_y += ((_PPUCTRL >> 1) & 1) * 240;

            // There are two background pattern table possibilities, use the one selected by PPUCTRL.
            ushort bg_pattern_base = (ushort)((_PPUCTRL & 0x10) == 0 ? 0x0000 : 0x1000);

            // 0 | 1
            // -----
            // 2 | 3
            int which_nametable = 0;

            // nametable_x/y are the coordinates relative to the top-left of whichever nametable this pixel is inside
            int nametable_x = (ppu_scroll_x + px) % 512;
            if (nametable_x >= 256)
            {
                which_nametable += 1;
                nametable_x -= 256;
            }

            int nametable_y = (ppu_scroll_y + py) % 480;
            if (nametable_y >= 240)
            {
                which_nametable += 2;
                nametable_y -= 240;
            }

            int nametable_tile_x = nametable_x / 8;
            int nametable_tile_y = nametable_y / 8;

            int attribute_x = nametable_tile_x / 4;
            int attribute_y = nametable_tile_y / 4;

            // Compute palette number

            // The attribute table start depends on which nametable we're in.
            // (Start of Nametables in memory + each nametable is 1K + 0x3C0 bytes until the start of the AT)
            int nametable_start = 0x2000 + 0x400 * which_nametable;

            int attribute_table_base = nametable_start + 0x3C0;
            int attributeTableIndex = attribute_y * 8 + attribute_x;
            byte attributeTableEntry = readRAM((ushort)(attribute_table_base + attributeTableIndex));

            // which pallete to derive the color from
            int which_palette = 0;
            which_palette |= nametable_tile_x % 4 >= 2 ? 2 : 0;
            which_palette |= nametable_tile_y % 4 >= 2 ? 4 : 0;

            // Use the previous value to select the two-bit palette number
            byte palette_num = (byte)((attributeTableEntry >> which_palette) & 3);

            // Compute color index

            int nt_index = nametable_tile_y * 32 + nametable_tile_x;
            ushort pt_index = readRAM((ushort)(nametable_start + nt_index)); // TODO : Handle nametable mirroring properly

            int tile_x = nametable_x % 8;
            int tile_y = nametable_y % 8;

            byte lowBits = getPatternTable((ushort)(bg_pattern_base + pt_index * 16 + tile_y));
            byte highBits = getPatternTable((ushort)(bg_pattern_base + pt_index * 16 + tile_y + 8));

            byte lowBit = (byte)((lowBits >> (7 - tile_x)) & 1);
            byte highBit = (byte)((highBits >> (7 - tile_x)) & 1);
            byte color_index = (byte)(lowBit + highBit * 2);

            // If it's the background color, look at palette 0 (Universally-shared BG color)
            if (color_index == 0) palette_num = 0;

            byte RGB_index = readRAM((ushort)(0x3F00 + 4 * palette_num + color_index));
            uint pixelColor = RGBA_PALETTE[RGB_index & 0x3F];
            _imageData[256 * py + px] = pixelColor;

        }

        byte[] oam_temp = new byte[8];
        int sprite_count = 0;

        private void computeSpritesForScanline(int scanline)
        {
            bool mode816 = (_PPUCTRL & 0x20) != 0;
            sprite_count = 0;
            for (byte oam_index = 0; oam_index < 64 && sprite_count < 8; ++oam_index)
            {
                byte sprite_y = OAM[oam_index * 4 + 0];
                int max_y_offset = mode816 ? 16 : 8;

                if (scanline >= sprite_y && (scanline < sprite_y + max_y_offset))
                {
                    oam_temp[sprite_count] = oam_index;
                    sprite_count++;
                }
            }
        }

        private void drawSprites(int px, int py)
        {
            uint universalBackgroundColor = RGBA_PALETTE[readRAM(0x3F00)];

            // Are we drawing 8x16 sprites?
            bool mode816 = (_PPUCTRL & 0x20) != 0;

            int image_index = py * 256 + px;

            if ((_PPUMASK & 4) == 0 && px < 8) return;

            // Check each sprite 
            for (int spr_index = 0; spr_index < sprite_count; ++spr_index)
            {
                int oam_index = oam_temp[spr_index];

                byte sprite_y = OAM[oam_index * 4 + 0];
                byte pt_index = OAM[oam_index * 4 + 1];
                byte attributes = (byte)(OAM[oam_index * 4 + 2] & 0xE3); // There are several bits that are "unimplemented" (always return zero during read)
                byte sprite_x = OAM[oam_index * 4 + 3];

                byte palette_number = (byte)(attributes & 3);
                bool inFrontOfBG = (attributes & 0x20) == 0;
                bool flipHorizontal = (attributes & 0x40) != 0;
                bool flipVertical = (attributes & 0x80) != 0;

                // Sprites are never drawn on if their y-coord is 0
                if (sprite_y == 0 || sprite_y >= 240)
                    continue;

                if (px - sprite_x >= 8 || px < sprite_x)
                    continue;

                if (mode816)
                {
                    int which_tile = 0;
                    if (py - sprite_y >= 8)
                        which_tile = 1;
                    if (flipVertical)
                        which_tile = 1 - which_tile;

                    int ti = px - sprite_x;
                    int tj = (py - sprite_y) % 8; // Tiles are only 8x8, so handle when we're looking at the second tile

                    int i = flipHorizontal ? 7 - ti : ti;
                    int j = flipVertical ? 7 - tj : tj;

                    ushort sprite_pattern_table_base = (ushort)((pt_index & 1) == 0 ? 0x0000 : 0x1000);
                    pt_index = (byte)((pt_index & 0xFE) + which_tile);
                    byte lowBits = getPatternTable((ushort)(sprite_pattern_table_base + pt_index * 16 + j));
                    byte highBits = getPatternTable((ushort)(sprite_pattern_table_base + pt_index * 16 + j + 8));

                    byte lowBit = (byte)((lowBits >> (7 - i)) & 1);
                    byte highBit = (byte)((highBits >> (7 - i)) & 1);
                    byte color_index = (byte)(lowBit + highBit * 2);

                    if (color_index > 0)
                    {
                        // TODO : Respect BG/Sprite precedence
                        if (inFrontOfBG || _imageData[image_index] == universalBackgroundColor)
                        {
                            // Sprite Zero Hit?
                            if (oam_index == 0 && _imageData[image_index] != universalBackgroundColor)
                                _PPUSTATUS |= 0x40;

                            uint RGB_index = readRAM((ushort)(0x3F10 + palette_number * 4 + color_index));
                            uint pixelColor = RGBA_PALETTE[RGB_index & 0x3F];

                            _imageData[image_index] = pixelColor;
                        }
                    }

                }
                else
                {
                    int ti = px - sprite_x;
                    int tj = py - sprite_y;

                    int i = flipHorizontal ? 7 - ti : ti;
                    int j = flipVertical ? 7 - tj : tj;

                    ushort sprite_pattern_table_base = (ushort)((_PPUCTRL & 8) == 0 ? 0x0000 : 0x1000);
                    byte lowBits = getPatternTable((ushort)(sprite_pattern_table_base + pt_index * 16 + j));
                    byte highBits = getPatternTable((ushort)(sprite_pattern_table_base + pt_index * 16 + j + 8));

                    byte lowBit = (byte)((lowBits >> (7 - i)) & 1);
                    byte highBit = (byte)((highBits >> (7 - i)) & 1);
                    byte color_index = (byte)(lowBit + highBit * 2);

                    if (color_index > 0)
                    {
                        // TODO : Respect BG/Sprite precedence
                        if (inFrontOfBG || _imageData[image_index] == universalBackgroundColor)
                        {
                            // Sprite Zero Hit?
                            if (oam_index == 0 && _imageData[image_index] != universalBackgroundColor)
                                _PPUSTATUS |= 0x40;

                            uint RGB_index = readRAM((ushort)(0x3F10 + palette_number * 4 + color_index));
                            uint pixelColor = RGBA_PALETTE[RGB_index & 0x3F];

                            _imageData[image_index] = pixelColor;
                        }
                    }
                }

            }
        }

        public void assembleImage(int px, int py)
        {
            // clear the image
            //for (int i = 0; i < _imageData.Length; ++i)
            //    _imageData[i] = 0x000000FF;

            // Should we draw the background?
            if ((_PPUMASK & 8) != 0)
                drawBackground(px, py);

            // Should we draw the sprites?
            if ((_PPUMASK & 0x10) != 0)
                drawSprites(px, py);

        }

        private byte[] RAM = new byte[0x4000];
        private byte[] OAM = new byte[0x400];

        // Current column and scanline (int to support pre-scanline convention -1, etc.)
        private int x, scanline;

        #region Registers

        byte _PPUCTRL;
        byte _PPUMASK;
        byte _PPUSTATUS;

        ushort _PPUSCROLL;
        ushort _PPUADDR;
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

            if (x == 256)
            {
                computeSpritesForScanline(scanline + 1);
            }

            if (scanline >= 0 && scanline < 240 && x > 0 && x <= 256)
            {
                // There are 256 columns to the image, but the first PPU tick of the scanline does nothing, so delay by 1
                assembleImage(x - 1, scanline);
            }

            // At the very start of the VBlank region, let the CPU know.
            if (scanline == 241 && x == 1)
                _PPUSTATUS |= 0x80;

            // Potentially trigger NMI at the start of VBlank
            if ((byte)(_PPUCTRL & 0x80) != 0 && scanline == 241 && x == 1)
            {
                console.cpu.nmi = true;
            }

            x++;

            // Variable line width for the pre-scanline depending on even/odd frame
            int columns_this_frame = oddFrame && scanline == -1 ? 341 : 340;
            if (x == columns_this_frame + 1)
            {
                scanline++;
                x = 0;
            }

            if (scanline == 261)
            {
                // We're no longer in VBlank region
                _PPUSTATUS = (byte)(_PPUSTATUS & 0x7F);

                // Clear Sprite Zero Hit
                _PPUSTATUS &= 0xBF;

                scanline = -1;
                _frameCount++;
                oddFrame = _frameCount % 2 == 1;
            }
        }

        public void step()
        {
            render_step();
        }

        public void coldBoot()
        {
            _PPUCTRL = _PPUMASK = _PPUSTATUS = 0;
            _OAMADDR = 0;
            _PPUSCROLL = _PPUDATA = 0;
            _PPUADDR = 0;
            oddFrame = false;
        }

        public void warmBoot()
        {
            // AFAIK These are effectively the same except for some extremely exceptional cases
            coldBoot();
        }

        // Abstract grabbing data from the pattern tables since it could either be in VRAM, or directly mapped to CHR-ROM via the mapper
        private byte getPatternTable(ushort address)
        {
            if (console.mapper.mapsCHR())
            {
                return console.mapper.readCHR(address);
            }
            return readRAM(address);
        }

        private byte readRAM(ushort addr)
        {
            // Handle nametable mirroring
            if (addr >= 0x2000 && addr <= 0x2FFF)
            {
                if (console.cartridge.mapperControlsNametableMirroring)
                {
                    // TODO : Handle MMC1/3 and others that control nametable mirroring 
                }
                else
                {
                    if (console.cartridge.nametableIsVerticalMirrored)
                    {
                        return addr >= 0x2800 ? RAM[addr - 0x800] : RAM[addr];
                    }
                    else
                    {
                        return RAM[addr & 0xFBFF];
                    }
                }
            }

            return RAM[addr];
        }

        private void writeRAM(ushort addr, byte val)
        {
            RAM[addr] = val;
        }

        byte PPUDATA_ReadBuffer = 0;

        public byte read(ushort addr)
        {
            // Handle mirroring
            addr = (ushort)(0x2000 + (addr & 0x0007));

            if (addr == 0x2001)
            {
                return _PPUMASK;
            }
            else if (addr == 0x2002)
            {
                // Reading the status register clears the "NMI Occurred" flag, but still returns
                // the old value of the register before clearing the flag.
                byte result = _PPUSTATUS;
                _PPUSTATUS &= 0x7F;
                return result;
            }
            else if (addr == 0x2007)
            {
                ushort readAddress = (ushort)(_PPUADDR & 0x3FFF);

                byte returnValue;
                // Reads below 0x3F00 actually return the contents of an internal read buffer.
                if (readAddress < 0x3F00)
                {
                    returnValue = PPUDATA_ReadBuffer;
                    PPUDATA_ReadBuffer = readRAM((ushort)(_PPUADDR & 0x3FFF));
                }
                else
                {
                    returnValue = readRAM((ushort)(_PPUADDR & 0x3FFF));
                }

                int increment = (_PPUCTRL & 4) == 0 ? 1 : 32;
                _PPUADDR = (ushort)((_PPUADDR + increment) & 0xFFFF);

                return returnValue;
            }
            Console.WriteLine(string.Format("Unimplemented read to PPU @ {0:X}", addr));
            return 0;
        }

        public void write(ushort addr, byte val)
        {
            if (addr >= 0x2000 && addr <= 0x2007)
            {
                // Handle mirroring
                addr = (ushort)(0x2000 + (addr & 0x0007));

                if (addr == 0x2000)
                {
                    _PPUCTRL = val;
                }
                else if (addr == 0x2001)
                {
                    _PPUMASK = val;
                }
                else if (addr == 0x2003)
                {
                    _OAMADDR = val;
                }
                else if (addr == 0x2005)
                {
                    _PPUSCROLL = (ushort)((_PPUSCROLL << 8) | val);
                }
                else if (addr == 0x2006)
                {
                    // The user can set a VRAM address to write to by writing to PPUADDR twice in succession.
                    _PPUADDR = (ushort)((_PPUADDR << 8) | (val & 0xFF));
                }
                else if (addr == 0x2007)
                {
                    // Write val to the location pointed to by PPUADDR
                    writeRAM((ushort)(_PPUADDR & 0x3FFF), val);

                    // PPUADDR increments by a configurable amount stored in PPUCTRL
                    int increment = (_PPUCTRL & 4) == 0 ? 1 : 32;
                    _PPUADDR = (ushort)((_PPUADDR + increment) & 0x3FFF);
                }
            }
            else if (addr == 0x4014)
            {
                // Writing XX to 0x4014 causes CPU RAM 0xXX00-0xXXFF to be written to OAM
                for (int offset = 0; offset <= 0xFF; ++offset)
                {
                    ushort read_address = (ushort)((val << 8) + ((offset + _OAMADDR) & 0xFF));
                    OAM[offset] = console.memory.read8(read_address);
                }
            }
            else
            {
                Console.Error.WriteLine("Unimplemented write {0:X2} to PPU @ {1:X4}", val, addr);
            }
        }
    }
}
