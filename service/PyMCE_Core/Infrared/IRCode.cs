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
using System.Globalization;
using System.Text;
namespace PyMCE_Core.Infrared
{
    /// <summary>
    /// Encapsulates an MCE compatible IR Code.
    /// </summary>
    internal class IRCode
    {
        #region Constants

        /// <summary>
        /// This code does not use a carrier wave.
        /// </summary>
        public const int CarrierFrequencyDCMode = 0;

        /// <summary>
        /// Default carrier frequency, 36kHz (the carrier frequency for RC5, RC6 and RC-MM).
        /// </summary>
        public const int CarrierFrequencyDefault = 36000;

        /// <summary>
        /// The carrier frequency for this code is Unknown.
        /// </summary>
        public const int CarrierFrequencyUnknown = -1;

        /// <summary>
        /// How long the longest IR Code space should be (in microseconds).
        /// </summary>
        private const int LongestSpace = -75000;

        #endregion Constants

        #region Properties

        /// <summary>
        /// Gets or Sets the IR carrier frequency.
        /// </summary>
        public int Carrier { get; set; }

        /// <summary>
        /// Gets or Sets the IR timing data.
        /// </summary>
        public int[] TimingData { get; set; }

        #endregion Properties

        #region Constructors

        public IRCode()
            : this(CarrierFrequencyUnknown, new int[] { })
        {
        }

        public IRCode(int carrier)
            : this(carrier, new int[] { })
        {
        }

        public IRCode(int[] timingData)
            : this(CarrierFrequencyUnknown, timingData)
        {
        }

        public IRCode(int carrier, int[] timingData)
        {
            Carrier = carrier;
            TimingData = timingData;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Locates the gap between button presses and reduces the data down to just the first press.
        /// </summary>
        /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
        public bool FinalizeData()
        {
            if (TimingData.Length == 0)
                return false;

            // Find long spaces and trim the IR code ...
            var newData = new List<int>();
            foreach (int time in TimingData)
            {
                if (time <= LongestSpace)
                {
                    newData.Add(LongestSpace);
                    break;
                }
                else
                {
                    newData.Add(time);
                }
            }

            TimingData = newData.ToArray();
            return true;
        }

        /// <summary>
        /// Add timing data to this IR Code.
        /// </summary>
        /// <param name="timingData">Addition timing data.</param>
        public void AddTimingData(int[] timingData)
        {
            var newTimingData = new List<int>();

            int index = 0;

            if (TimingData.Length > 1)
            {
                for (index = 0; index < TimingData.Length - 1; index++)
                    newTimingData.Add(TimingData[index]);
            }
            else if (TimingData.Length == 0)
            {
                TimingData = new int[timingData.Length];
                timingData.CopyTo(TimingData, 0);
                return;
            }

            if (timingData.Length == 0 || index >= TimingData.Length)
                return;

            if (Math.Sign(timingData[0]) == Math.Sign(TimingData[index]))
            {
                newTimingData.Add(TimingData[index] + timingData[0]);

                for (index = 1; index < timingData.Length; index++)
                    newTimingData.Add(timingData[index]);
            }
            else
            {
                newTimingData.Add(TimingData[index]);
                newTimingData.AddRange(timingData);
            }

            TimingData = newTimingData.ToArray();
        }

        /// <summary>
        /// Creates a byte array representation of this IR Code.
        /// </summary>
        /// <returns>Byte array representation (internally it is in Pronto format).</returns>
        public byte[] ToByteArray()
        {
            var output = new StringBuilder();

            ushort[] prontoData = Pronto.ConvertIrCodeToProntoRaw(this);

            for (var index = 0; index < prontoData.Length; index++)
            {
                output.Append(prontoData[index].ToString("X4"));
                if (index != prontoData.Length - 1)
                    output.Append(' ');
            }

            return Encoding.ASCII.GetBytes(output.ToString());
        }

        #endregion Methods

        #region Static Methods

        /// <summary>
        /// Creates an IrCode object from old IR file bytes.
        /// </summary>
        /// <param name="data">IR file bytes.</param>
        /// <returns>New IrCode object.</returns>
        private static IRCode FromOldData(byte[] data)
        {
            var timingData = new List<int>();

            var len = 0;

            foreach (byte curByte in data)
            {
                if ((curByte & 0x80) != 0)
                    len += (curByte & 0x7F);
                else
                    len -= curByte;

                if ((curByte & 0x7F) != 0x7F)
                {
                    timingData.Add(len * 50);
                    len = 0;
                }
            }

            if (len != 0)
                timingData.Add(len * 50);

            var newCode = new IRCode(timingData.ToArray());
            newCode.FinalizeData();
            // Seems some old files have excessively long delays in them .. this might fix that problem ...

            return newCode;
        }

        /// <summary>
        /// Creates an IrCode object from Pronto format file bytes.
        /// </summary>
        /// <param name="data">IR file bytes.</param>
        /// <returns>New IrCode object.</returns>
        private static IRCode FromProntoData(byte[] data)
        {
            var code = Encoding.ASCII.GetString(data);

            var stringData = code.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var prontoData = new ushort[stringData.Length];
            for (var i = 0; i < stringData.Length; i++)
                prontoData[i] = ushort.Parse(stringData[i], NumberStyles.HexNumber);

            IRCode newCode = Pronto.ConvertProntoDataToIrCode(prontoData);
            if (newCode != null)
                newCode.FinalizeData();
            // Seems some old files have excessively long delays in them .. this might fix that problem ...

            return newCode;
        }

        /// <summary>
        /// Create a new IrCode object from byte array data.
        /// </summary>
        /// <param name="data">Byte array to create from.</param>
        /// <returns>New IrCode object.</returns>
        public static IRCode FromByteArray(byte[] data)
        {
            return data[4] == ' ' ? FromProntoData(data) : FromOldData(data);
        }

        #endregion Static Methods
    }
}
