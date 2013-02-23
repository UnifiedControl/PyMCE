#region License
// PyMCE - Python MCE IR Receiver Library
// Copyright 2012-2013 Dean Gardiner <gardiner91@gmail.com>
//
// Some portions of code and files are from 'IR-Server-Suite'
// Copyright 2005-2009 Team MediaPortal - http://www.team-mediaportal.com
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//
// http://www.gnu.org/copyleft/gpl.html
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace PyMCE.Core.Infrared
{
    /// <summary>
    /// Philips Pronto interface class.
    /// </summary>
    public static class Pronto
    {
        #region Enumerations

        /// <summary>
        /// Pronto IR Code type identifier.
        /// </summary>
        public enum CodeFormat
        {
            // Supported ...
            RawOscillated = 0x0000,
            RawUnmodulated = 0x0100,
            RC5 = 0x5000,
            RC5X = 0x5001,
            RC6 = 0x6000,
            RC6A = 0x6001,

            // Unsupported ...
            VariableLength = 0x7000,
            IndexToUDB = 0x8000,
            NEC_1 = 0x9000,
            NEC_2 = 0x900A,
            NEC_3 = 0x900B,
            NEC_4 = 0x900C,
            NEC_5 = 0x900D,
            NEC_6 = 0x900E,
            YamahaNEC = 0x9001,
        }

        #endregion Enumerations

        #region Constants

        private const int CarrierRC5 = 36000;
        private const int CarrierRC6 = 36000;

        /// <summary>
        /// Pronto clock multiplier.
        /// </summary>
        private const double ProntoClock = 0.241246;

        /*
        const int CarrierITT      = 0;
        const int CarrierJVC      = 38000;
        const int CarrierNEC      = 38000;
        const int CarrierNrc17    = 38000;
        const int CarrierRCA      = 56000;
        const int CarrierRCMM     = 36000;
        const int CarrierRecs80   = 38000;
        const int CarrierSharp    = 38000;
        const int CarrierSirc     = 40000;
        const int CarrierXSat     = 38000;
        */

        private const int SignalFree = 10000;
        private const int SignalFreeRC6 = 2700;
        private static readonly int[] RC6AHeader = new int[] { 3150, -900, 450, -450, 450, -450, 450, -900, 450, -900 };
        private static readonly int[] RC6Header = new int[] { 2700, -900, 450, -900, 450, -450, 450, -450, 450, -900 };

        #endregion Constants

        #region Public Methods

        /*
    /// <summary>
    /// Write Pronto data to a file.
    /// </summary>
    /// <param name="fileName">File to write Pronto data to.</param>
    /// <param name="prontoData">Pronto data to write.</param>
    public static void WriteProntoFile(string fileName, ushort[] prontoData)
    {
      if (String.IsNullOrEmpty(fileName))
        throw new ArgumentNullException("fileName");

      if (prontoData == null || prontoData.Length == 0)
        throw new ArgumentNullException("prontoData");

      using (StreamWriter file = new StreamWriter(fileName, false))
      {
        for (int index = 0; index < prontoData.Length; index++)
        {
          file.Write(String.Format("{0:X4}", prontoData[index]));
          if (index != prontoData.Length - 1)
            file.Write(' ');
        }
      }
    }
    */

        /// <summary>
        /// Converts an IR Code represented in Pronto data to an IrCode object.
        /// </summary>
        /// <param name="prontoData">The Pronto data to convert.</param>
        /// <returns>IrCode object of interpretted Pronto data.</returns>
        public static IRCode ConvertProntoDataToIrCode(ushort[] prontoData)
        {
            if (prontoData == null || prontoData.Length == 0)
                throw new ArgumentNullException("prontoData");

            switch ((CodeFormat)prontoData[0])
            {
                case CodeFormat.RawOscillated:
                case CodeFormat.RawUnmodulated:
                    return ConvertProntoRawToIrCode(prontoData);

                case CodeFormat.RC5:
                    return ConvertProntoRC5ToIrCode(prontoData);

                case CodeFormat.RC5X:
                    return ConvertProntoRC5XToIrCode(prontoData);

                case CodeFormat.RC6:
                    return ConvertProntoRC6ToIrCode(prontoData);

                case CodeFormat.RC6A:
                    return ConvertProntoRC6AToIrCode(prontoData);

                default:
                    return null;
            }
        }

        private static IRCode ConvertProntoRawToIrCode(ushort[] prontoData)
        {
            var length = prontoData.Length;
            if (length < 5)
                return null;

            var prontoCarrier = prontoData[1];
            if (prontoCarrier == 0x0000)
                prontoCarrier = ConvertToProntoCarrier(IRCode.CarrierFrequencyDefault);

            var carrier = prontoCarrier * ProntoClock;

            var firstSeq = 2 * prontoData[2];
            var repeatSeq = 2 * prontoData[3];

            var timingData = new List<int>();

            var pulse = true;
            var repeatCount = 0;
            var start = 4;
            var done = false;

            var index = start;
            var sequence = firstSeq;

            if (firstSeq == 0)
            {
                if (repeatSeq == 0)
                    return null;

                sequence = repeatSeq;
                repeatCount = 1;
            }

            while (!done)
            {
                var time = (int)(prontoData[index] * carrier);

                if (pulse)
                    timingData.Add(time);
                else
                    timingData.Add(-time);

                index++;
                pulse = !pulse;

                if (index != start + sequence) continue;

                switch (repeatCount)
                {
                    case 0:
                        if (repeatSeq != 0)
                        {
                            start += firstSeq;
                            sequence = repeatSeq;
                            index = start;
                            pulse = true;
                            repeatCount++;
                        }
                        else
                            done = true;
                        break;

                    case 1:
                        done = true;
                        break;

                    default:
                        index = start;
                        pulse = true;
                        repeatCount++;
                        break;
                }
            }

            return new IRCode(ConvertFromProntoCarrier(prontoCarrier), timingData.ToArray());
        }

        private static IRCode ConvertProntoRC5ToIrCode(ushort[] prontoData)
        {
            if (prontoData.Length != 6)
                return null;

            if (prontoData[0] != (ushort)CodeFormat.RC5)
                return null;

            var prontoCarrier = prontoData[1];
            if (prontoCarrier == 0x0000)
                prontoCarrier = ConvertToProntoCarrier(CarrierRC5);

            if (prontoData[2] + prontoData[3] != 1)
                return null;

            var system = prontoData[4];
            var command = prontoData[5];

            if (system > 31)
                return null;

            if (command > 127)
                return null;

            ushort rc5 = 0;

            rc5 |= (1 << 13); // Start Bit 1

            if (command < 64)
                rc5 |= (1 << 12); // Start Bit 2

            rc5 |= (1 << 11); // Toggle Bit

            rc5 |= (ushort)((system << 6) | command);

            var timingData = new List<int>();

            var currentTime = 0;

            for (var i = 13; i > 0; i--)
            {
                if ((rc5 & (1 << i)) != 0) // Logic 1 (Space, Pulse)
                {
                    if (currentTime > 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime -= 900;
                    timingData.Add(currentTime);

                    currentTime = 900;
                }
                else // Logic 0 (Pulse, Space)
                {
                    if (currentTime < 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime += 900;
                    timingData.Add(currentTime);

                    currentTime = -900;
                }
            }

            if (currentTime > 0)
            {
                timingData.Add(currentTime);
                timingData.Add(-SignalFree);
            }
            else
            {
                timingData.Add(currentTime - SignalFree);
            }

            return new IRCode(ConvertFromProntoCarrier(prontoCarrier), timingData.ToArray());
        }

        private static IRCode ConvertProntoRC5XToIrCode(ushort[] prontoData)
        {
            if (prontoData.Length != 7)
                return null;

            if (prontoData[0] != (ushort)CodeFormat.RC5X)
                return null;

            var prontoCarrier = prontoData[1];
            if (prontoCarrier == 0x0000)
                prontoCarrier = ConvertToProntoCarrier(CarrierRC5);

            if (prontoData[2] + prontoData[3] != 2)
                return null;

            var system = prontoData[4];
            var command = prontoData[5];
            var data = prontoData[6];

            if (system > 31)
                return null;

            if (command > 127)
                return null;

            if (data > 63)
                return null;

            uint rc5 = 0;

            rc5 |= (1 << 19); // Start Bit 1

            if (command < 64)
                rc5 |= (1 << 18); // Start Bit 2 (Inverted Command Bit 6)

            rc5 |= (1 << 17); // Toggle Bit

            rc5 |= (uint)((system << 12) | (command << 6) | data);

            var timingData = new List<int>();
            var currentTime = 0;

            for (var i = 19; i > 0; i--)
            {
                if (i == 11)
                {
                    if (currentTime < 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }
                    currentTime += 3600;
                }

                if ((rc5 & (1 << i)) != 0) // Logic 1 (S, P)
                {
                    if (currentTime > 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime -= 900;
                    timingData.Add(currentTime);

                    currentTime = 900;
                }
                else // Logic 0 (P, S)
                {
                    if (currentTime < 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime += 900;
                    timingData.Add(currentTime);

                    currentTime = -900;
                }
            }

            if (currentTime > 0)
            {
                timingData.Add(currentTime);
                timingData.Add(-SignalFree);
            }
            else
            {
                timingData.Add(currentTime - SignalFree);
            }

            return new IRCode(ConvertFromProntoCarrier(prontoCarrier), timingData.ToArray());
        }

        private static IRCode ConvertProntoRC6ToIrCode(ushort[] prontoData)
        {
            if (prontoData.Length != 6)
                return null;

            if (prontoData[0] != (ushort)CodeFormat.RC6)
                return null;

            var prontoCarrier = prontoData[1];
            if (prontoCarrier == 0x0000)
                prontoCarrier = ConvertToProntoCarrier(CarrierRC6);

            if (prontoData[2] + prontoData[3] != 1)
                return null;

            var system = prontoData[4];
            var command = prontoData[5];

            if (system > 255)
                return null;

            if (command > 255)
                return null;

            var rc6 = (ushort)((system << 8) | command);

            var timingData = new List<int>();
            timingData.AddRange(RC6Header);

            var currentTime = 900; // Second half of Trailer Bit (0)

            for (var i = 16; i > 0; i--)
            {
                if ((rc6 & (1 << i)) != 0) // Logic 1 (S, P)
                {
                    if (currentTime > 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime -= 450;
                    timingData.Add(currentTime);

                    currentTime = 450;
                }
                else // Logic 0 (P, S)
                {
                    if (currentTime < 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime += 450;
                    timingData.Add(currentTime);

                    currentTime = -450;
                }
            }

            if (currentTime > 0)
            {
                timingData.Add(currentTime);
                timingData.Add(-SignalFreeRC6);
            }
            else
            {
                timingData.Add(currentTime - SignalFreeRC6);
            }

            return new IRCode(ConvertFromProntoCarrier(prontoCarrier), timingData.ToArray());
        }

        private static IRCode ConvertProntoRC6AToIrCode(ushort[] prontoData)
        {
            if (prontoData.Length != 6)
                return null;

            if (prontoData[0] != (ushort)CodeFormat.RC6A)
                return null;

            var prontoCarrier = prontoData[1];
            if (prontoCarrier == 0x0000)
                prontoCarrier = ConvertToProntoCarrier(CarrierRC6);

            if (prontoData[2] + prontoData[3] != 2)
                return null;

            var customer = prontoData[5];
            var system = prontoData[5];
            var command = prontoData[6];

            if (system > 255)
                return null;

            if (command > 255)
                return null;

            if (customer > 127 && customer < 32768)
                return null;

            var rc6 = (uint)((customer << 16) | (system << 8) | command);

            var timingData = new List<int>();
            timingData.AddRange(RC6AHeader);

            var currentTime = 900;

            for (var i = ((customer >= 32768) ? 32 : 24); i > 0; i--)
            {
                if ((rc6 & (1 << i)) != 0) // Logic 1 (S, P)
                {
                    if (currentTime > 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime -= 450;
                    timingData.Add(currentTime);

                    currentTime = 450;
                }
                else // Logic 0 (P, S)
                {
                    if (currentTime < 0)
                    {
                        timingData.Add(currentTime);
                        currentTime = 0;
                    }

                    currentTime += 450;
                    timingData.Add(currentTime);

                    currentTime = -450;
                }
            }

            if (currentTime > 0)
            {
                timingData.Add(currentTime);
                timingData.Add(-SignalFreeRC6);
            }
            else
            {
                timingData.Add(currentTime - SignalFreeRC6);
            }

            return new IRCode(ConvertFromProntoCarrier(prontoCarrier), timingData.ToArray());
        }

        /*
        public static ushort[] ConvertIrCodeToPronto(IrCode irCode)
        {
          CodeFormat codeFormat;
          Int64 value;

          if (Decode(irCode, out codeFormat, out value))
            return EncodePronto(codeFormat, value);
          else
            return null;
        }
        */

        /// <summary>
        /// Converts the ir code into Pronto raw format.
        /// </summary>
        /// <param name="irCode">The ir code to convert.</param>
        /// <returns>Pronto data (raw format).</returns>
        public static ushort[] ConvertIrCodeToProntoRaw(IRCode irCode)
        {
            CodeFormat codeFormat;

            var irCodeCarrier = IRCode.CarrierFrequencyDefault;

            switch (irCode.Carrier)
            {
                case IRCode.CarrierFrequencyDCMode:
                    codeFormat = CodeFormat.RawUnmodulated;
                    irCodeCarrier = IRCode.CarrierFrequencyDefault;
                    break;

                case IRCode.CarrierFrequencyUnknown:
                    codeFormat = CodeFormat.RawOscillated;
                    irCodeCarrier = IRCode.CarrierFrequencyDefault;
                    break;

                default:
                    codeFormat = CodeFormat.RawOscillated;
                    irCodeCarrier = irCode.Carrier;
                    break;
            }

            var prontoCarrier = ConvertToProntoCarrier(irCodeCarrier);
            var carrier = prontoCarrier * ProntoClock;

            var prontoData = irCode.TimingData.Select(Math.Abs)
                .Select(duration => (ushort) Math.Round(duration/carrier))
                .ToList();

            if (prontoData.Count % 2 != 0)
                prontoData.Add(SignalFree);

            var burstPairs = (ushort)(prontoData.Count / 2);

            prontoData.Insert(0, (ushort)codeFormat); // Pronto Code Type
            prontoData.Insert(1, prontoCarrier); // IR Frequency
            prontoData.Insert(2, burstPairs); // First Burst Pairs
            prontoData.Insert(3, 0x0000); // Repeat Burst Pairs

            return prontoData.ToArray();
        }

        /// <summary>
        /// Converts from a Pronto format carrier frequency to an integer format.
        /// </summary>
        /// <param name="prontoCarrier">The Pronto format carrier.</param>
        /// <returns>The carrier frequency as an integer number.</returns>
        public static int ConvertFromProntoCarrier(ushort prontoCarrier)
        {
            return (int)(1000000 / (prontoCarrier * ProntoClock));
        }

        /// <summary>
        /// Converts from an integer number carrier frequency to a Pronto carrier format.
        /// </summary>
        /// <param name="carrierFrequency">The integer carrier frequency.</param>
        /// <returns>The carrier frequency in Pronto format.</returns>
        public static ushort ConvertToProntoCarrier(int carrierFrequency)
        {
            return (ushort)(1000000 / (carrierFrequency * ProntoClock));
        }

        #endregion Public Methods
    }
}
