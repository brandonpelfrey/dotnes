using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using DotNES.Utilities;
using NAudio.Wave;

namespace DotNES.Core
{
    public class APU
    {
        Logger log = new Logger("APU");

        public APU()
        {
            this.audioBuffer = new AudioBuffer();
            NESWaveProvider nesWaveProvider = new NESWaveProvider(audioBuffer);
            waveOut = new WaveOut();
            waveOut.Init(nesWaveProvider);
        }

        private AudioBuffer audioBuffer;
        private WaveOut waveOut;

        public void setLoggerEnabled(bool enable)
        {
            this.log.setEnabled(enable);
        }

        Pulse PULSE_ONE = new Pulse();
        Pulse PULSE_TWO = new Pulse();
        Triangle TRIANGLE = new Triangle();
        Noise NOISE = new Noise();
        Dmc DMC = new Dmc();

        FrameCounterMode FRAME_COUNTER_MODE;
        bool IRQ_INHIBIT;

        public byte read(ushort addr)
        {
            // TODO: Support read to 0x4015 (I believe this is the only valid register read for the APU)
            return 0;
        }

        // Register description for APU http://wiki.nesdev.com/w/index.php/APU
        public void write(ushort addr, byte val)
        {
            // Write to a pulse settings register
            if (addr >= 0x4000 && addr <= 0x4007)
            {
                Pulse pulse;
                if (addr < 0x4004)
                {
                    pulse = PULSE_ONE;
                }
                else
                {
                    pulse = PULSE_TWO;
                }
                int normalizedAddress = addr & 0x3;
                switch (normalizedAddress)
                {
                    case 0: // 0x4000 or 0x4004
                        pulse.DUTY = (byte)((val >> 6) & 0x3);
                        pulse.LENGTH_COUNTER_HALT = ((val >> 5) & 0x1) == 1;
                        pulse.CONSTANT_VOLUME = ((val >> 4) & 0x1) == 1;
                        pulse.ENVELOPE_DIVIDER_PERIOD_OR_VOLUME = (byte)(val & 0xF);
                        pulse.envelope_volume = 15;
                        pulse.envelope_counter = pulse.ENVELOPE_DIVIDER_PERIOD_OR_VOLUME;
                        break;
                    case 1: // 0x4001 or 0x4005
                        pulse.SWEEP_ENABLED = ((val >> 7) & 0x1) == 1;
                        pulse.SWEEP_PERIOD = (byte)((val >> 4) & 0x7);
                        pulse.sweep_period_counter = pulse.SWEEP_PERIOD;
                        pulse.SWEEP_NEGATE = ((val >> 3) & 0x1) == 1;
                        pulse.SWEEP_SHIFT = (byte)((val >> 4) & 0x7);
                        break;
                    case 2: // 0x4002 or 0x4006
                        pulse.TIMER = (ushort)((pulse.TIMER & 0xFF00) | val);
                        break;
                    case 3: // 0x4003 or 0x4007
                        pulse.LENGTH_COUNTER_LOAD = (byte)((val >> 3) & 0x1F);
                        pulse.current_length_counter = lengthCounterLookupTable[pulse.LENGTH_COUNTER_LOAD];
                        pulse.TIMER = (ushort)((pulse.TIMER & 0x00FF) | ((val & 0x7) << 8));
                        break;
                    default:
                        break;
                }
            }
            else
            {
                // All other register writes
                switch (addr)
                {
                    case 0x4008:
                        TRIANGLE.LENGTH_COUNTER_HALT = (((val >> 7) & 0x1) == 1);
                        TRIANGLE.LINEAR_COUNTER_LOAD = (byte)(val & 0x7F);
                        break;
                    case 0x400A:
                        TRIANGLE.TIMER = (ushort)((TRIANGLE.TIMER & 0xFF00) | val);
                        break;
                    case 0x400B:
                        TRIANGLE.LENGTH_COUNTER_LOAD = (byte)((val >> 3) & 0x1F);
                        TRIANGLE.TIMER = (ushort)((TRIANGLE.TIMER & 0x00FF) | ((val & 0x7) << 8));
                        break;
                    case 0x400C:
                        NOISE.ENVELOPE_LOOP = (((val >> 5) & 0x1) == 1);
                        NOISE.CONSTANT_VOLUME = (((val >> 4) & 0x1) == 1);
                        NOISE.VOLUME_ENVELOP = (byte)(val & 0xF);
                        break;
                    case 0x400E:
                        NOISE.LOOP_NOISE = (((val >> 7) & 0x1) == 1);
                        NOISE.NOISE_PERIOD = (byte)(val & 0xF);
                        break;
                    case 0x400F:
                        NOISE.LENGTH_COUNTER_LOAD = (byte)((val >> 3) & 0xF);
                        break;
                    case 0x4010:
                        DMC.IRQ_ENABLE = (((val >> 7) & 0x1) == 1);
                        DMC.LOOP = (((val >> 6) & 0x1) == 1);
                        DMC.FREQUENCY = (byte)(val & 0xF);
                        break;
                    case 0x4011:
                        DMC.LOAD_COUNTER = (byte)(val & 0x7F);
                        break;
                    case 0x4012:
                        DMC.SAMPLE_ADDRESS = val;
                        break;
                    case 0x4013:
                        DMC.SAMPLE_LENGTH = val;
                        break;
                    case 0x4015:
                        DMC.ENABLED = (((val >> 4) & 0x1) == 1);
                        NOISE.ENABLED = (((val >> 3) & 0x1) == 1);
                        TRIANGLE.ENABLED = (((val >> 2) & 0x1) == 1);
                        PULSE_TWO.ENABLED = (((val >> 1) & 0x1) == 1);
                        PULSE_ONE.ENABLED = (((val >> 0) & 0x1) == 1);
                        break;
                    case 0x4017:
                        FRAME_COUNTER_MODE = (((val >> 7) & 0x1) == 1) ? FrameCounterMode.FIVE_STEP : FrameCounterMode.FOUR_STEP;
                        IRQ_INHIBIT = (((val >> 6) & 0x1) == 1);
                        break;
                    default:
                        log.error("Attempting to write to unknown address {0:X4}", addr);
                        break;
                }
            }


        }

        int extraCpuCycle = 0;
        public void step(int cpuCycles)
        {
            cpuCycles += extraCpuCycle;
            for (int i = 0; i < cpuCycles / 2; i++)
                apuStep();
            extraCpuCycle = cpuCycles & 0x1;
        }

        private int apuStepCounter = 0;

        //Execute a APU frame counter tick after these number apu ticks
        //Documentation http://wiki.nesdev.com/w/index.php/APU_Frame_Counter
        private void apuStep()
        {
            apuStepCounter++;
            switch (apuStepCounter)
            {
                case 3728:
                case 7456:
                case 11185:
                    APUFrameTick();
                    break;
                case 14914:
                    APUFrameTick();
                    apuStepCounter = 0;
                    break;
                default:
                    break;
            }
        }

        const int sampleRate = 48000;
        const int samplesPerFrame = 800;
        const int samplesPerAPUFrameTick = samplesPerFrame / 4;
        int timeInSamples = 0;
        int apuFrameTicksTillAudio = 40;

        public void writeFrameCounterAudio()
        {
            //Audio fails if we start right away, so waiting to build audio buffer for 10 frames
            //TODO handle better
            if (apuFrameTicksTillAudio > -1)
            {
                apuFrameTicksTillAudio--;
            }
            if (apuFrameTicksTillAudio == 0)
            {
                waveOut.Play();
            }

            for (int i = 0; i < samplesPerAPUFrameTick; i++)
            {
                float pulseOne = getPulseAudio(PULSE_ONE, timeInSamples);
                float pulseTwo = getPulseAudio(PULSE_TWO, timeInSamples);
                float tri = getTriangleAudio(TRIANGLE, timeInSamples);

                //TODO we can't just add these together, should use actual or approximation of actual mixer
                audioBuffer.write(pulseOne + pulseTwo + tri);
                timeInSamples++;
            }

            if (timeInSamples > 10000000)
            {
                //TODO handle this better, probably will cause a pop every million samples (~200 seconds)
                timeInSamples = 0;
            }
        }

        bool tickLengthCounterAndSweep = false;

        // TODO support 4 step and 5 step modes (assumes 4 step mode)
        // Documentation http://wiki.nesdev.com/w/index.php/APU
        private void APUFrameTick()
        {
            if (tickLengthCounterAndSweep)
            {
                tickLengthCounter(PULSE_ONE);
                tickLengthCounter(PULSE_TWO);
                tickSweep(PULSE_ONE);
                tickSweep(PULSE_TWO);
            }

            tickLinearCounter(TRIANGLE);
            tickEnvelopCounter(PULSE_ONE);
            tickEnvelopCounter(PULSE_TWO);


            writeFrameCounterAudio();
            tickLengthCounterAndSweep = !tickLengthCounterAndSweep;
        }

        private void tickEnvelopCounter(Pulse pulse)
        {
            if (pulse.envelope_counter == 0)
            {
                if (pulse.envelope_volume == 0)
                {
                    if(pulse.ENVELOPE_LOOP) {
                        pulse.envelope_volume = 15;
                    }
                }
                else
                {
                    pulse.envelope_volume--;
                }
                pulse.envelope_counter = pulse.ENVELOPE_DIVIDER_PERIOD_OR_VOLUME;
            }
            else
            {
                pulse.envelope_counter--;
            }
        }

        private void tickLengthCounter(Pulse pulse)
        {
            if (!pulse.LENGTH_COUNTER_HALT)
            {
                pulse.current_length_counter -= 1;
                if (pulse.current_length_counter < 0)
                {
                    pulse.current_length_counter = 0;
                }
            }
        }

        private void tickSweep(Pulse pulse)
        {
            if (pulse.sweep_period_counter == 0) {
                if (pulse.SWEEP_ENABLED)
                {
                    int periodAdjustment = pulse.TIMER >> pulse.SWEEP_SHIFT;
                    int newPeriod;
                    if (pulse.SWEEP_NEGATE)
                    {
                        newPeriod = pulse.TIMER - periodAdjustment;
                    }
                    else
                    {
                        newPeriod = (pulse.TIMER + periodAdjustment);
                    }
                    if (0 < newPeriod && newPeriod < 0x7FF && pulse.SWEEP_SHIFT != 0)
                    {
                        pulse.TIMER = (ushort)newPeriod;
                    }
                }
                pulse.sweep_period_counter = pulse.SWEEP_PERIOD;
            }
            else
            {
                pulse.sweep_period_counter--;
            }
        }

        private void tickLinearCounter(Triangle triangle)
        {
            if (!triangle.LENGTH_COUNTER_HALT)
            {
                if (triangle.LINEAR_COUNTER_LOAD == 0)
                {
                    triangle.LINEAR_COUNTER_LOAD = 0;
                }
                else
                {
                    triangle.LINEAR_COUNTER_LOAD -= 1;
                }
            }
        }

        // Ignoring phase of the signal for now 
        // Documentation on values http://wiki.nesdev.com/w/index.php/APU_Pulse 
        double[] dutyMap = new double[] { .125, .25, .5, .75 };

        public float getPulseAudio(Pulse pulse, int timeInSamples)
        {
            if (!pulse.ENABLED || pulse.current_length_counter == 0)
            {
                return 0.0f;
            }

            //Frequency is the clock speed of the CPU ~ 1.7MH divided by 16 divied by the timer.
            //TODO pretty much everything here, only looking at frequency flag right now
            double frequency = 106250.0 / pulse.TIMER;
            double normalizedSampleTime = timeInSamples * frequency / sampleRate;

            double fractionalNormalizedSampleTime = normalizedSampleTime - Math.Floor(normalizedSampleTime);
            float dutyPulse = fractionalNormalizedSampleTime < dutyMap[pulse.DUTY] ? 1 : -1;

            byte volume = pulse.ENVELOPE_DIVIDER_PERIOD_OR_VOLUME;
            if (!pulse.CONSTANT_VOLUME)
            {
                volume = pulse.envelope_volume;
            }

            return dutyPulse * volume / 15;
        }

        public float getTriangleAudio(Triangle triangle, int timeInSamples)
        {
            if (!triangle.ENABLED || triangle.LINEAR_COUNTER_LOAD == 0)
            {
                return 0.0f;
            }

            // Triangle plays one octave lower than the given frequency
            double frequency = 106250.0 / triangle.TIMER / 2;
            double normalizedSampleTime = timeInSamples * frequency / sampleRate;

            // Given the frequency, determine where we are inside a single triangle waveform as a point in [0,1]
            //  1      /\
            //        /  \
            //  0 ---/----\----...
            //      /      \  /
            // -1  /        \/
            //    0    .5    1   ..
            float normalized = ((timeInSamples * (int)frequency) % sampleRate) / (float)sampleRate;

            // Map [0,1) to the triangle in range [-1,1]
            if (normalized <= 0.5)
                return -1f + 4f * normalized;
            else
                return 3f - 4f * normalized;
        }

        // Values to load for the length counter as documented here http://wiki.nesdev.com/w/index.php/APU_Length_Counter
        byte[] lengthCounterLookupTable = new byte[] { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14, 12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };
    }

    public class AudioBuffer
    {
        float[] audioRingBuffer = new float[1 << 16];
        ushort startPointer = 0;
        ushort nextSamplePointer = 0;

        public void write(float value)
        {
            audioRingBuffer[nextSamplePointer] = value;
            nextSamplePointer++;
        }

        public int copyToArray(float[] audioBuff, int offset, int numSamples)
        {
            if (startPointer < nextSamplePointer)
            {
                ushort amountToCopy = (ushort)Math.Min(nextSamplePointer - startPointer, numSamples);
                copy(audioRingBuffer, startPointer, audioBuff, offset, amountToCopy);
                startPointer += amountToCopy;
                return amountToCopy;
            }
            else if (nextSamplePointer < startPointer)
            {
                int amountAfter = Math.Min(audioRingBuffer.Length - startPointer, numSamples);
                copy(audioRingBuffer, startPointer, audioBuff, offset, amountAfter);
                numSamples -= amountAfter;

                int amountBefore = Math.Min(nextSamplePointer, numSamples);
                copy(audioRingBuffer, 0, audioBuff, offset + amountAfter, amountBefore);
                int floatsCopied = amountAfter + amountBefore;
                startPointer += (ushort)floatsCopied;
                return floatsCopied;
            }
            else
            {
                return 0;
            }
        }

        public void copy(float[] src, int srcOffset, float[] dest, int destOffset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                dest[destOffset + i] = src[srcOffset + i];
            }
        }
    }

    public class NESWaveProvider : IWaveProvider
    {
        private WaveFormat waveFormat;
        private AudioBuffer audioBuffer;

        public NESWaveProvider(AudioBuffer audioBuffer)
            : this(48000, 1)
        {
            this.audioBuffer = audioBuffer;
        }

        public NESWaveProvider(int sampleRate, int channels)
        {
            SetWaveFormat(sampleRate, channels);
        }

        public void SetWaveFormat(int sampleRate, int channels)
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
        }

        public int Read(float[] buffer, int offset, int sampleCount)
        {
            return audioBuffer.copyToArray(buffer, offset, sampleCount);
        }

        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }
    }

    public class Pulse
    {
        public bool ENABLED { get; set; } = true;

        // 0x4000 or 0x4004
        public byte DUTY { get; set; }                     // 2 bits
        public bool LENGTH_COUNTER_HALT { get; set; } = true;
        public bool CONSTANT_VOLUME { get; set; }
        public byte ENVELOPE_DIVIDER_PERIOD_OR_VOLUME { get; set; }  // 4 bits

        // 0x4001 or 0x4005
        public bool SWEEP_ENABLED { get; set; }
        public byte SWEEP_PERIOD { get; set; }             // 3 bits
        public bool SWEEP_NEGATE { get; set; }
        public byte SWEEP_SHIFT { get; set; }              // 3 bits

        // 0x4002 - 0x4003 or 0x4006 - 0x4007
        public ushort TIMER { get; set; }                  // 11 bits
        public byte LENGTH_COUNTER_LOAD { get; set; }      // 5 bits

        // flag is shared with LENGTH_COUNTER_HALT 
        public bool ENVELOPE_LOOP { get { return this.LENGTH_COUNTER_HALT; } }

        public int current_length_counter { get; set; } = 1;
        public int sweep_period_counter { get; set; }
        public int envelope_counter { get; set; }
        public byte envelope_volume { get; set; }
    }

    public class Triangle
    {
        public bool ENABLED { get; set; }

        // 0x4008
        public bool LENGTH_COUNTER_HALT { get; set; }
        public byte LINEAR_COUNTER_LOAD { get; set; }      // 7 bits

        // 0x400A - 0x400B
        public ushort TIMER { get; set; }                  // 11 bits
        public byte LENGTH_COUNTER_LOAD { get; set; }      // 5 bits
    }

    public class Noise
    {
        public bool ENABLED { get; set; }

        // 0x400C
        public bool ENVELOPE_LOOP { get; set; }
        public bool CONSTANT_VOLUME { get; set; }
        public byte VOLUME_ENVELOP { get; set; }           // 4 bits

        // 0x400D
        public bool LOOP_NOISE { get; set; }
        public byte NOISE_PERIOD { get; set; }             // 4 bits

        // 0x400F
        public byte LENGTH_COUNTER_LOAD { get; set; }      // 5 bits
    }

    public class Dmc
    {
        public bool ENABLED { get; set; }

        // 0x4010
        public bool IRQ_ENABLE { get; set; }
        public bool LOOP { get; set; }
        public byte FREQUENCY { get; set; }                // 4 bits

        // 0x4011
        public byte LOAD_COUNTER { get; set; }             // 7 bits

        // 0x4012
        public byte SAMPLE_ADDRESS { get; set; }           // 8 bits

        // 0x4013
        public byte SAMPLE_LENGTH { get; set; }            // 8 bits
    }

    enum FrameCounterMode
    {
        FOUR_STEP = 0,
        FIVE_STEP = 1
    }
}
