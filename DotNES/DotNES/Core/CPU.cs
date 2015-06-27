using DotNES.Core;
using DotNES.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNES
{
    /// <summary>
    /// This is an emulator for the Ricoh 2A03 processor, which is a MOS 6502 derivative lacking Decimal mode.
    /// Lots of great information is available or http://wiki.nesdev.com/w/index.php/CPU
    /// </summary>
    public class CPU
    {
        private Logger log = new Logger();

        private MethodInfo[] opcodeFunctions;
        
        Memory memory;

        #region Registers

        /// <summary>
        /// Accumulator Register - All arithmetic operations (Add, Subtract, etc.) act on this register.
        /// </summary>
        byte _A;

        /// <summary>
        /// Index Register X - Used as a pointer offset in several indirect addressing modes
        /// </summary>
        byte _X;

        /// <summary>
        /// Index Register Y - Used as a pointer offset in several indirect addressing modes
        /// </summary>
        byte _Y;

        /// <summary>
        /// Program Counter Register - 16-bit pointer to the currently executing piece of code.
        /// Incremented as most instructions are processed. It is manipulated by jump-type instructions, as well as interrupts.
        /// </summary>
        ushort _PC;

        /// <summary>
        /// Stack Pointer Register - The 2A03 can refernce 256 bytes of stack space. This register points to the current point in the stack.
        /// </summary>
        byte _S;

        /// <summary>
        /// Processor Status Register - This is a bitfield representing certain flags, each being set by various operations during execution.
        /// The various fields of the status register from most-significant to least-significant: 
        /// NVssDIZC
        /// 
        /// N : Negative:  1 if the previous operation resulted in a negative value
        /// V : Overflow:  1 if the previous caused a signed overflow
        /// s : (Unused):  These have effectively no use on the NES. They are written during certain stack operations.
        /// D : Decimal:   1 if Decimal Mode is enabled. This is ignored in the 2A03 and so it is of no concern on the NES.
        /// I : Interrupt: "Interrupt Inhibit" (0 : IRQ and NMI interrupts will get through, 1 : Only NMI will get through)
        /// Z : Zero:      Set if the last operation resulted in a zero
        /// C : Carry:     Set if the last addition or shift resulted in a carry, or last subtraction resulted in no borrow.
        /// </summary>
        byte _P;

        enum StatusFlag
        {
            Carry = 0,
            Zero = 1,
            InterruptDisable = 2,
            Decimal = 3,
            Unused1 = 4,
            Unused2 = 5,
            Overflow = 6,
            Negative = 7
        }

        // A couple of helpers for getting and setting bits for various registers
        void setFlag(StatusFlag flag, byte val)
        {
            int bit = (int)flag;

            // Get rid of the bit currently there, and put our bit there.
            _P = (byte)((_P & (~(1 << bit))) | ((val & 1) << bit));
        }

        byte getFlag(StatusFlag flag)
        {
            int bit = (int)flag;
            return (byte)((_P >> bit) & 1);
        }

        #endregion

        #region OpCodes
        
        private void initializeOpCodeTable()
        {
            // Initialize all functions to zero-cycle NOPs
            opcodeFunctions = new MethodInfo[256];
            for(int i=0; i<256; ++i)
                opcodeFunctions[i] = null;

            // Look at all the OpCodes implemented in this class and insert them into the array
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach(var method in methods)
            {
                OpCodeAttribute attribute = Attribute.GetCustomAttribute(method, typeof(OpCodeAttribute), false) as OpCodeAttribute;
                if (attribute == null)
                    continue;

                opcodeFunctions[attribute.opcode] = method;
            }

            log.info("Initialized 2A03 with {0} implemented OpCodes", opcodeFunctions.Where(x => x != null).Count());
        }

        // Implementation details : http://www.obelisk.demon.co.uk/6502/reference.html

        #region Jumps
        [OpCode(opcode = 0x4C, name ="JMP")]
        private int JMP_Absolute()
        {
            _PC = argOne16();
            return 3;
        }

        [OpCode(opcode = 0x6C, name = "JMP")]
        private int JMP_Indirect()
        {
            _PC = memory.read16(argOne16());
            return 5;
        }

        [OpCode(opcode = 0x20, name = "JSR")]
        private int JSR_Absolute()
        {
            pushStack16((ushort)(_PC + 3));
            _PC = argOne16();
            return 6;
        }

        #endregion

        #region Arithmetic

        #endregion

        #region Flag Manipulation
        [OpCode(opcode = 0xD8, name = "CLD")]
        private int CLD_Implicit()
        {
            setFlag(StatusFlag.Decimal, 0);
            _PC += 1;
            return 2;
        }
        #endregion

        #region Stack
        [OpCode(opcode = 0x48, name = "PHA")]
        private int PHA()
        {
            pushStack8(_A);
            _PC += 1;
            return 3;
        }

        [OpCode(opcode = 0x08, name = "PHP")]
        private int PHP()
        {
            pushStack8(_P);
            _PC += 1;
            return 3;
        }

        [OpCode(opcode = 0x68, name = "PLA")]
        private int PLA()
        {
            _A = popStack8();
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 1;
            return 4;
        }

        [OpCode(opcode = 0x28, name = "PLP")]
        private int PLP()
        {
            _P = popStack8();
            _PC += 1;
            return 4;
        }
        #endregion

        #region Store

        #region STA
        [OpCode(opcode = 0x85, name = "STA")]
        private int STA_ZeroPage()
        {
            memory.write8(argOne(), _A);
            _PC += 2;
            return 3;
        }

        [OpCode(opcode = 0x95, name = "STA")]
        private int STA_ZeroPageX()
        {
            memory.write8((ushort)( ( argOne() + _X ) & 0xFF ), _A);
            _PC += 2;
            return 4;
        }

        [OpCode(opcode = 0x8D, name = "STA")]
        private int STA_Absolute()
        {
            memory.write8(argOne16(), _A);
            _PC += 3;
            return 4;
        }

        [OpCode(opcode = 0x9D, name = "STA")]
        private int STA_AbsoluteX()
        {
            memory.write8((ushort)(argOne16() + _X), _A);
            _PC += 3;
            return 5;
        }

        [OpCode(opcode = 0x99, name = "STA")]
        private int STA_AbsoluteY()
        {
            memory.write8((ushort)(argOne16() + _Y), _A);
            _PC += 3;
            return 5;
        }

        [OpCode(opcode = 0x81, name = "STA")]
        private int STA_IndirectX()
        {
            ushort address = (ushort) ((argOne() + _X) & 0xFF);
            ushort indirectAddress = memory.read16(address);
            memory.write8(indirectAddress, _A);
            _PC += 2;
            return 6;
        }

        [OpCode(opcode = 0x91, name = "STA")]
        private int STA_IndirectY()
        {
            ushort address = argOne();
            ushort indirectAddress = (ushort) (memory.read16(address) + _Y);
            memory.write8(indirectAddress, _A);
            _PC += 2;
            return 6;
        }


        #endregion

        #region STX

        [OpCode(opcode = 0x86, name = "STX")]
        private int STX_ZeroPage()
        {
            memory.write8(argOne(), _X);
            _PC += 2;
            return 3;
        }

        [OpCode(opcode = 0x96, name = "STX")]
        private int STX_ZeroPageY()
        {
            memory.write8((ushort) ((argOne() + _Y) & 0xFF), _X);
            _PC += 2;
            return 4;
        }

        [OpCode(opcode = 0x8E, name = "STX")]
        private int STX_Absolute()
        {
            memory.write8(argOne16(), _X);
            _PC += 3;
            return 4;
        }

        #endregion

        #region STY

        [OpCode(opcode = 0x84, name = "STY")]
        private int STY_ZeroPage()
        {
            memory.write8(argOne(), _Y);
            _PC += 2;
            return 3;
        }

        [OpCode(opcode = 0x94, name = "STY")]
        private int STY_ZeroPageX()
        {
            memory.write8((ushort)((argOne() + _X) & 0xFF), _Y);
            _PC += 2;
            return 4;
        }

        [OpCode(opcode = 0x8C, name = "STY")]
        private int STY_Absolute()
        {
            memory.write8(argOne16(), _Y);
            _PC += 3;
            return 4;
        }

        #endregion

        #endregion

        #region Load

        #region LDA

        [OpCode(opcode = 0xA9, name = "LDA")]
        private int LDA_Immediate()
        {
            _A = argOne();
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 2;
            return 2;
        }

        [OpCode(opcode = 0xA5, name = "LDA")]
        private int LDA_ZeroPage()
        {
            _A = memory.read8(argOne());
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 2;
            return 3;
        }

        [OpCode(opcode = 0xB5, name = "LDA")]
        private int LDA_ZeroPageX()
        {
            ushort address = (ushort)((argOne() + _X) & 0xFF);
            _A = memory.read8(address);
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 2;
            return 4;
        }

        [OpCode(opcode = 0xAD, name = "LDA")]
        private int LDA_Absolute()
        {
            _A = memory.read8(argOne16());
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 3;
            return 4;
        }

        [OpCode(opcode = 0xBD, name = "LDA")]
        private int LDA_AbsoluteX()
        {
            return LDA_AbsoluteWithRegister(_X);
        }

        [OpCode(opcode = 0xB9, name = "LDA")]
        private int LDA_AbsoluteY()
        {
            return LDA_AbsoluteWithRegister(_Y);
        }

        private int LDA_AbsoluteWithRegister(ushort registerValue)
        {
            ushort arg = argOne16();
            ushort address = (ushort)(arg + registerValue);
            _A = memory.read8(address);

            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 3;

            if (samePage(arg ,address))
            {
                return 4; 
            }
            else
            {
                return 5;
            }
        }

        [OpCode(opcode = 0xA1, name = "LDA")]
        private int LDA_IndirectX()
        {
            ushort address = (ushort)((argOne() + _X) & 0xFF);
            _A = memory.read8(memory.read16(address));
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 2;
            return 6;
        }

        [OpCode(opcode = 0xB1, name = "LDA")]
        private int LDA_IndirectY()
        {
            ushort addressWithoutY = memory.read16(argOne());
            ushort addressWithY = (ushort)(addressWithoutY + _Y);
            _A = memory.read8(memory.read16(addressWithY));

            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 2;

            if (samePage(addressWithY, addressWithoutY))
            {
                return 5;
            }
            else
            {
                return 6;
            }
        }
        #endregion

        #region LDX

        [OpCode(opcode = 0xA2, name = "LDX")]
        private int LDX_Immediate()
        {
            _X = argOne();
            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 2;
            return 2;
        }

        [OpCode(opcode = 0xA6, name = "LDX")]
        private int LDX_ZeroPage()
        {
            _X = memory.read8(argOne());
            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 2;
            return 3;
        }

        [OpCode(opcode = 0xB6, name = "LDX")]
        private int LDX_ZeroPageY()
        {
            ushort address = (ushort)(memory.read16(argOne()) + _Y);
            _X = memory.read8(address);
            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 2;
            return 4;
        }

        [OpCode(opcode = 0xAE, name = "LDX")]
        private int LDX_Absolute()
        {
            _X = memory.read8(argOne16());
            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 3;
            return 4;
        }

        [OpCode(opcode = 0xBE, name = "LDX")]
        private int LDX_AbsoluteY()
        {
            ushort arg = argOne16();
            ushort address = (ushort)(arg + _Y);
            _X = memory.read8(address);

            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 3;

            if (samePage(arg, address))
            {
                return 4;
            }
            else
            {
                return 5;
            }
        }

        #endregion

        #region LDY

        [OpCode(opcode = 0xA0, name = "LDY")]
        private int LDY_Immediate()
        {
            _Y = argOne();
            setZeroForOperand(_Y);
            setNegativeForOperand(_Y);
            _PC += 2;
            return 2;
        }

        [OpCode(opcode = 0xA4, name = "LDX")]
        private int LDY_ZeroPage()
        {
            _Y = memory.read8(argOne());
            setZeroForOperand(_Y);
            setNegativeForOperand(_Y);
            _PC += 2;
            return 3;
        }

        [OpCode(opcode = 0xB4, name = "LDX")]
        private int LDY_ZeroPageX()
        {
            ushort address = (ushort)((argOne() + _X) & 0xFF);
            _Y = memory.read8(address);
            setZeroForOperand(_Y);
            setNegativeForOperand(_Y);
            _PC += 2;
            return 4;
        }

        [OpCode(opcode = 0xAC, name = "LDX")]
        private int LDY_Absolute()
        {
            _Y = memory.read8(argOne16());
            setZeroForOperand(_Y);
            setNegativeForOperand(_Y);
            _PC += 3;
            return 4;
        }

        [OpCode(opcode = 0xBC, name = "LDX")]
        private int LDY_AbsoluteX()
        {
            ushort arg = argOne16();
            ushort address = (ushort)(arg + _X);
            _Y = memory.read8(address);

            setZeroForOperand(_Y);
            setNegativeForOperand(_Y);
            _PC += 3;

            if (samePage(arg, address))
            {
                return 4;
            }
            else
            {
                return 5;
            }
        }

        #endregion

        #endregion

        #region Transfer
        [OpCode(opcode = 0xAA, name = "TAX")]
        private int TAX()
        {
            _X = _A;
            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 1;
            return 2;
        }

        [OpCode(opcode = 0xA8, name = "TAY")]
        private int TAY()
        {
            _Y = _A;
            setZeroForOperand(_Y);
            setNegativeForOperand(_Y);
            _PC += 1;
            return 2;
        }

        [OpCode(opcode = 0xBA, name = "TSX")]
        private int TSX()
        {
            _X = _S;
            setZeroForOperand(_X);
            setNegativeForOperand(_X);
            _PC += 1;
            return 2;
        }

        [OpCode(opcode = 0x8A, name = "TXA")]
        private int TXA()
        {
            _A = _X;
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 1;
            return 2;
        }

        [OpCode(opcode = 0x9A, name = "TXS")]
        private int TXS()
        {
            _S = _X;
            setZeroForOperand(_S);
            setNegativeForOperand(_S);
            _PC += 1;
            return 2;
        }

        [OpCode(opcode = 0x98, name = "TYA")]
        private int TYA()
        {
            _A = _Y;
            setZeroForOperand(_A);
            setNegativeForOperand(_A);
            _PC += 1;
            return 2;
        }

        #endregion

        #region OpcodeHelpers
        private byte argOne()
        {
            return memory.read8((ushort)(_PC + 1));
        }

        private ushort argOne16()
        {
            return memory.read16((ushort)(_PC + 1));
        }

        private byte argTwo()
        {
            return memory.read8((ushort)(_PC + 2));
        }

        private void pushStack16(ushort val)
        {
            _S -= 2;
            memory.write16(stackAddressOf(_S), val);
        }

        private ushort popStack16()
        {
            ushort val = memory.read16(stackAddressOf(_S));
            _S += 2;
            return val;
        }

        private void pushStack8(byte val)
        {
            _S -= 1;
            memory.write16(stackAddressOf(_S), val);
        }

        private byte popStack8()
        {
            byte val = memory.read8(stackAddressOf(_S));
            _S += 1;
            return val;
        }

        private ushort stackAddressOf(byte stackPointer)
        {
            return (ushort)(0x0100 + stackPointer);
        }

        private bool samePage(ushort addressOne, ushort addressTwo)
        {
            return ((addressOne ^ addressTwo) & 0xFF00) == 0;
        }

        private void setZeroForOperand(byte operand) {
            setFlag(StatusFlag.Zero, operand == 0 ? (byte)1 : (byte)0);
        }

        private void setNegativeForOperand(byte operand) {
            setFlag(StatusFlag.Negative, (byte)( ( operand >> 7 ) ) );
        }
        #endregion

        #endregion

        #region System Startup and Reset

        /// <summary>
        /// Set up the CPU to be in the state it would be after a normal power cycle.
        /// Details sampled from physical hardware: http://wiki.nesdev.com/w/index.php/CPU_power_up_state
        /// </summary>
        public void coldBoot()
        {
            _P = 0x34;
            _A = _X = _Y = 0;
            _S = 0xFD;

            // Set up some of the memory-mapped stuff
            memory.write8(0x4017, 0x00); // (frame irq enabled)
            memory.write8(0x4015, 0x00); // (all channels disabled)
        }

        /// <summary>
        /// Set up the CPU to be in the state it would be after the reset line is held high. (e.g. User presses the RESET button on an NES)
        /// Details sampled from physical hardware: http://wiki.nesdev.com/w/index.php/CPU_power_up_state
        /// </summary>
        public void warmBoot()
        {
            // A, X, Y were not affected


            // S was decremented by 3(but nothing was written to the stack)
            _S -= 3;

            // The I (IRQ disable) flag was set to true(status ORed with $04)
            setFlag(StatusFlag.InterruptDisable, 1);

            // Silence the APU
            memory.write8(0x4015, 0x00);
        }

        #endregion

        public CPU(Memory memory)
        {
            this.memory = memory;

            initializeOpCodeTable();
        }

        /// <summary>
        /// Execute the next CPU instruction and return the number of cycles used.
        /// </summary>
        /// <returns></returns>
        public int step()
        {
            byte opcode = memory.read8(_PC);

            MethodInfo opcodeMethod = opcodeFunctions[opcode];
            if(opcodeMethod == null)
            {
                log.error("Unknown opcode {0:X} encountered.", opcode);
                return 0;
            }

            return (int)opcodeMethod.Invoke(this, null);
        }
    }
}
