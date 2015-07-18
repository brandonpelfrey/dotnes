using DotNES.Utilities;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Core
{
    public class IO
    {
        private Logger log = new Logger("IO");
        public void setLoggerEnabled(bool enable)
        {
            this.log.setEnabled(enable);
        }

        // Status register for writes to 0x4016. Used to signal the start of reading controller input
        // http://wiki.nesdev.com/w/index.php/Standard_controller
        byte _S;

        private bool readControllerData = false;
        private static Dictionary<int, String> controllerIndexToNameDictionary = new Dictionary<int, string>
        { {0, "A" },
          {1, "B" },
          {2, "Select" },
          {3, "Start" },
          {4, "Up" },
          {5, "Down" },
          {6, "Left" },
          {7, "Right" } };

        ControllerInputState controllerOneInputState;
        ControllerInputState controllerTwoInputState;

        private Controller playerOneController;
        private Controller playerTwoController;

        public IO(Controller playerOneController, Controller playerTwoController)
        {
            this.playerOneController = playerOneController;
            this.playerTwoController = playerTwoController;
        }

        // Standard controller documentation
        // http://wiki.nesdev.com/w/index.php/Standard_controller
        public void write(ushort address, byte val)
        {
            if (address == 0x4016)
            {
                byte previous_S = _S;
                _S = val;

                // Changing the last bit of _S from 1 to 0 indicates the start of reading controller input
                if ((previous_S & 0x1) == 1 && (_S & 0x1) == 0)
                {
                    loadButtonStates();
                    readControllerData = true;
                }
            }
            else
            {
                log.error("Unimplemented write to Controller @ {0:X}", address);
            }
        }

        public byte read(ushort address)
        {
            if (address == 0x4016)
            {
                byte controllerInput = controllerOneInputState.next();
                if (controllerInput == 1)
                {
                    log.info("Player One Controller pressed {0}", controllerIndexToNameDictionary[controllerOneInputState.index - 1]);
                }
                return controllerInput;
            }
            else if (address == 0x4017)
            {
                byte controllerInput = controllerTwoInputState.next();
                if (controllerInput == 1)
                {
                    log.info("Player Two Controller pressed {0}", controllerIndexToNameDictionary[controllerTwoInputState.index - 1]);
                }
                return controllerInput;
            }
            else
            {
                log.error("Unsupported read of address {0:X4}", address);
                return 0;
            }
        }

        private void loadButtonStates()
        {
            controllerOneInputState = new ControllerInputState(extractButtonStates(playerOneController));
            controllerTwoInputState = new ControllerInputState(extractButtonStates(playerTwoController));
        }

        private bool[] extractButtonStates(Controller controller)
        {
            return new bool[]{ controller.getA(),
                controller.getB(),
                controller.getSelect(),
                controller.getStart(),
                controller.getUp(),
                controller.getDown(),
                controller.getLeft(),
                controller.getRight() };
        }

        private class ControllerInputState
        {
            bool[] states;
            public int index { get; private set; }

            public ControllerInputState(bool[] states)
            {
                this.states = states;
                this.index = 0;
            }

            public byte next()
            {
                if (index < states.Length)
                {
                    byte val = (byte)(states[index] ? 1 : 0);
                    index++;
                    return val;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    public class KeyboardController : Controller
    {
        public static Controller DEFAULT_PLAYER_ONE_CONTROLLER = new KeyboardController(Keys.J, Keys.K, Keys.RightShift, Keys.Enter, Keys.W, Keys.S, Keys.A, Keys.D);
        public static Controller DEFAULT_PLAYER_TWO_CONTROLLER = new KeyboardController(Keys.NumPad1, Keys.NumPad2, Keys.NumPad4, Keys.NumPad5, Keys.Up, Keys.Down, Keys.Left, Keys.Right);

        private Keys a;
        private Keys b;
        private Keys select;
        private Keys start;
        private Keys up;
        private Keys down;
        private Keys left;
        private Keys right;

        public KeyboardController(Keys a, Keys b, Keys select, Keys start, Keys up, Keys down, Keys left, Keys right)
        {
            this.a = a;
            this.b = b;
            this.select = select;
            this.start = start;
            this.up = up;
            this.down = down;
            this.left = left;
            this.right = right;
        }

        public bool getA()
        {
            return Keyboard.GetState().IsKeyDown(a);
        }

        public bool getB()
        {
            return Keyboard.GetState().IsKeyDown(b);
        }

        public bool getSelect()
        {
            return Keyboard.GetState().IsKeyDown(select);
        }

        public bool getStart()
        {
            return Keyboard.GetState().IsKeyDown(start);
        }

        public bool getUp()
        {
            return Keyboard.GetState().IsKeyDown(up);
        }

        public bool getDown()
        {
            return Keyboard.GetState().IsKeyDown(down);
        }

        public bool getLeft()
        {
            return Keyboard.GetState().IsKeyDown(left);
        }
        public bool getRight()
        {
            return Keyboard.GetState().IsKeyDown(right);
        }
    }

    public class FM2TASController : Controller
    {
        private NESConsole console;
        private int playerNumber;
        public int PlayerNumber {  get { return playerNumber; } }

        // RLDUTSBA
        private byte[] keysForFrame;

        public FM2TASController(string fm2Path, int playerNumber, NESConsole console)
        {
            this.playerNumber = playerNumber;
            this.console = console;

            string[] lines = File.ReadAllLines(fm2Path).Where(x => x.StartsWith("|")).ToArray();
            keysForFrame = new byte[lines.Length];

            int frame = 0;
            foreach(string line in lines)
            {
                byte keys = 0;
                if (line.Substring(3, 12).Contains("R")) keys |= 0x80;
                if (line.Substring(3, 12).Contains("L")) keys |= 0x40;
                if (line.Substring(3, 12).Contains("D")) keys |= 0x20;
                if (line.Substring(3, 12).Contains("U")) keys |= 0x10;
                if (line.Substring(3, 12).Contains("T")) keys |= 0x08;
                if (line.Substring(3, 12).Contains("S")) keys |= 0x04;
                if (line.Substring(3, 12).Contains("B")) keys |= 0x02;
                if (line.Substring(3, 12).Contains("A")) keys |= 0x01;

                keysForFrame[frame] = keys;
                frame++;
            }
        }

        private byte getPressedKeys()
        {
            long frame = console.ppu.FrameCount - 1;
            if (frame >= 0 && frame < keysForFrame.Length)
                return keysForFrame[frame];
            else
                return 0;
        }

        public bool getA()
        {
            return (getPressedKeys() & 0x01) != 0;
        }

        public bool getB()
        {
            return (getPressedKeys() & 0x02) != 0;
        }

        public bool getDown()
        {
            return (getPressedKeys() & 0x20) != 0;
        }

        public bool getLeft()
        {
            return (getPressedKeys() & 0x40) != 0;
        }

        public bool getRight()
        {
            return (getPressedKeys() & 0x80) != 0;
        }

        public bool getSelect()
        {
            return (getPressedKeys() & 0x04) != 0;
        }

        public bool getStart()
        {
            return (getPressedKeys() & 0x08) != 0;
        }

        public bool getUp()
        {
            return (getPressedKeys() & 0x10) != 0;
        }
    }

    public interface Controller
    {
        bool getA();
        bool getB();
        bool getSelect();
        bool getStart();
        bool getUp();
        bool getDown();
        bool getLeft();
        bool getRight();
    }
}
