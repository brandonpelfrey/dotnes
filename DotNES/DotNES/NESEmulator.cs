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
            Window.AllowUserResizing = false;

            initializeSystem();
        }

        private void initializeSystem()
        {
            cart = new Cartridge("C:\\roms\\dk.nes");
            system = new NESConsole(cart);
            system.cpu.coldBoot();
            system.ppu.setLoggerEnabled(false);
            system.cpu.setLoggerEnabled(false);
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

            spriteBatch.Begin();
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

        private void updateDrawTexture()
        {
            for (int i = 0; i < 256; ++i)
                for (int j = 0; j < 256; ++j)
                {
                    if (j < 240)
                        textureData[j * 256 + i] = system.ppu.ImageData[j * 256 + i];
                    else
                        textureData[j * 256 + i] = 0xFF000000;
                }
            texture.SetData(textureData);
        }

        private void frameAdvance()
        {
            long startingFrame = system.ppu.FrameCount;
            int steps = 0;
            while (true)
            {
                system.step();
                steps++;
                if (startingFrame != system.ppu.FrameCount)
                    break;
            }
            if(Window != null && Window.Title != null)
            Window.Title = string.Format("DotNES : {0} Instructions Executed Last Frame", steps);
        }
    }
}
