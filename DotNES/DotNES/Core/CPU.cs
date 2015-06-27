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
        public void JMP_Absolute()
        {
            // General OpCode Implementation
            // Perform OpCode Logic
            // Update any flags
            // Update PC based on jump address or opcode+operand size
        }
        #endregion

        #region Arithmetic

        #endregion

        #region Flag Manipulation
        [OpCode(name="CLD", opcode = 0xD8)]
        private int CLD_Implicit()
        {
            setFlag(StatusFlag.Decimal, 0);
            _PC += 1;
            return 2;
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
