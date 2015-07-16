using DotNES.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES
{
    public class NESEmulator : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private uint[] textureData;
        private Texture2D texture;

        Cartridge cart;
        NESConsole system;

        public NESEmulator()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 512;
            graphics.PreferredBackBufferHeight = 512;
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            initializeSystem();
        }

        private void initializeSystem()
        {
            cart = new Cartridge("C:\\roms\\dk.nes");
            system = new NESConsole(cart);
            system.cpu.coldBoot();
            system.ppu.setLoggerEnabled(false);
            system.cpu.setLoggerEnabled(false);
            system.io.setLoggerEnabled(false);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            texture = new Texture2D(GraphicsDevice, 256, 256);
            textureData = new uint[256 * 256];
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            int screenWidth = graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int screenHeight = graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;
            Rectangle screenRectangle = new Rectangle(0, 0, screenWidth, screenHeight);

            // Draw without any interpolation
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
            spriteBatch.Draw(texture, screenRectangle, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            frameAdvance();
            system.ppu.assembleImage();
            updateDrawTexture();

            base.Update(gameTime);
        }

        private uint reverseBytes(uint num)
        {
            return ((num >> 24) & 0xff) |
                   ((num << 8) & 0xff0000) |
                   ((num >> 8) & 0xff00) |
                   ((num << 24) & 0xff000000);

        }

        private void updateDrawTexture()
        {
            for (int i = 0; i < 256; ++i)
                for (int j = 0; j < 256; ++j)
                {
                    if (j < 240)
                        textureData[j * 256 + i] = reverseBytes(system.ppu.ImageData[j * 256 + i]);
                    else
                        textureData[j * 256 + i] = 0xFF000000;
                }
            texture.SetData(textureData);
        }

        private void frameAdvance()
        {
            long startingFrame = system.ppu.FrameCount;
            int steps = 0;

            system.apu.writeFrameAudio();

            while (true)
            {
                system.step();
                steps++;
                if (startingFrame != system.ppu.FrameCount)
                    break;
            }
            if (Window != null && Window.Title != null)
                Window.Title = string.Format("DotNES : Frame {0}", system.ppu.FrameCount);
        }
    }
}
