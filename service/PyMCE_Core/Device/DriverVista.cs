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

using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using PyMCE.Core.Infrared;

namespace PyMCE.Core.Device
{
    /// <summary>
    /// Driver class for the Windows Vista eHome driver.
    /// </summary>
    internal class DriverVista : Driver
    {
        #region Interop

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControl(
            SafeFileHandle handle,
            [MarshalAs(UnmanagedType.U4)] IoCtrl ioControlCode,
            IntPtr inBuffer, int inBufferSize,
            IntPtr outBuffer, int outBufferSize,
            out int bytesReturned,
            IntPtr overlapped);

        #endregion Interop

        #region Structures

        #region Notes

        // This is really weird and I don't know why this works, but apparently on
        // 64-bit systems the following structures require 64-bit integers.
        // The easiest way to do this is to use an IntPtr because it is 32-bits
        // wide on 32-bit systems, and 64-bits wide on 64-bit systems.
        // Given that it is exactly the same data on 32-bit or 64-bit systems it
        // makes no sense (to me) why Microsoft would do it this way ...

        // I couldn't find any reference to this in the WinHEC or other
        // documentation I have seen.  When 64-bit users started reporting
        // "The data area passed to a system call is too small." errors (122) the
        // only thing I could think of was that the structures were differenly
        // sized on 64-bit systems.  And the only thing in C# that sizes
        // differently on 64-bit systems is the IntPtr.


        // Chemelli:
        //
        // Unfortunately this is how it works :(
        // documented even at http://stackoverflow.com/questions/732642/using-64-bits-driver-from-32-bits-application
        //
        // Created interfaces for each struct and added a 32bit copy and a 64bit copy of those

        #endregion Notes

        #region Nested type: AvailableBlasters

        private interface IAvailableBlasters
        {
            object Blasters { get; set; }
        }

        /// <summary>
        /// Available Blasters data structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct AvailableBlasters32 : IAvailableBlasters
        {
            /// <summary>
            /// Blaster bit-mask.
            /// </summary>
            private System.Int32 _Blasters;

            public object Blasters
            {
                get { return _Blasters; }
                set { _Blasters = int.Parse((string) value); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AvailableBlasters64 : IAvailableBlasters
        {
            /// <summary>
            /// Blaster bit-mask.
            /// </summary>
            private System.Int64 _Blasters;

            public object Blasters
            {
                get { return _Blasters; }
                set { _Blasters = int.Parse(value.ToString()); }
            }
        }

        #endregion

        #region Nested type: DeviceCapabilities

        private interface IDeviceCapabilities
        {
            object ProtocolVersion { get; set; }
            object TransmitPorts { get; set; }
            object ReceivePorts { get; set; }
            object LearningMask { get; set; }
            object DetailsFlags { get; set; }
        }

        /// <summary>
        /// Device Capabilities data structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceCapabilities32 : IDeviceCapabilities
        {
            /// <summary>
            /// Device protocol version.
            /// </summary>
            public System.Int32 _ProtocolVersion;

            public object ProtocolVersion
            {
                get { return _ProtocolVersion; }
                set { _ProtocolVersion = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Number of transmit ports – 0-32.
            /// </summary>
            public System.Int32 _TransmitPorts;

            public object TransmitPorts
            {
                get { return _TransmitPorts; }
                set { _TransmitPorts = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Number of receive ports – 0-32. For beanbag, this is two (one for learning, one for normal receiving).
            /// </summary>
            public System.Int32 _ReceivePorts;

            public object ReceivePorts
            {
                get { return _ReceivePorts; }
                set { _ReceivePorts = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Bitmask identifying which receivers are learning receivers – low bit is the first receiver, second-low bit is the second receiver, etc ...
            /// </summary>
            public System.Int32 _LearningMask;

            public object LearningMask
            {
                get { return _LearningMask; }
                set { _LearningMask = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Device flags.
            /// </summary>
            public System.Int32 _DetailsFlags;

            public object DetailsFlags
            {
                get { return _DetailsFlags; }
                set { _DetailsFlags = int.Parse(value.ToString()); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceCapabilities64 : IDeviceCapabilities
        {
            /// <summary>
            /// Device protocol version.
            /// </summary>
            public System.Int64 _ProtocolVersion;

            public object ProtocolVersion
            {
                get { return _ProtocolVersion; }
                set { _ProtocolVersion = int.Parse((string) value); }
            }

            /// <summary>
            /// Number of transmit ports – 0-32.
            /// </summary>
            public System.Int64 _TransmitPorts;

            public object TransmitPorts
            {
                get { return _TransmitPorts; }
                set { _TransmitPorts = int.Parse((string) value); }
            }

            /// <summary>
            /// Number of receive ports – 0-32. For beanbag, this is two (one for learning, one for normal receiving).
            /// </summary>
            public System.Int64 _ReceivePorts;

            public object ReceivePorts
            {
                get { return _ReceivePorts; }
                set { _ReceivePorts = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Bitmask identifying which receivers are learning receivers – low bit is the first receiver, second-low bit is the second receiver, etc ...
            /// </summary>
            public System.Int64 _LearningMask;

            public object LearningMask
            {
                get { return _LearningMask; }
                set { _LearningMask = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Device flags.
            /// </summary>
            public System.Int64 _DetailsFlags;

            public object DetailsFlags
            {
                get { return _DetailsFlags; }
                set { _DetailsFlags = int.Parse(value.ToString()); }
            }
        }

        #endregion

        #region Nested type: ReceiveParams

        private interface IReceiveParams
        {
            object DataEnd { get; set; }
            object ByteCount { get; set; }
            object CarrierFrequency { get; set; }
        }

        /// <summary>
        /// Receive parameters.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct ReceiveParams32 : IReceiveParams
        {
            /// <summary>
            /// Last packet in block?
            /// </summary>
            public System.Int32 _DataEnd;

            public object DataEnd
            {
                get { return _DataEnd; }
                set { _DataEnd = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Number of bytes in block.
            /// </summary>
            public System.Int32 _ByteCount;

            public object ByteCount
            {
                get { return _ByteCount; }
                set { _ByteCount = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Carrier frequency of IR received.
            /// </summary>
            public System.Int32 _CarrierFrequency;

            public object CarrierFrequency
            {
                get { return _CarrierFrequency; }
                set { _CarrierFrequency = int.Parse(value.ToString()); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ReceiveParams64 : IReceiveParams
        {
            /// <summary>
            /// Last packet in block?
            /// </summary>
            public System.Int64 _DataEnd;

            public object DataEnd
            {
                get { return _DataEnd; }
                set { _DataEnd = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Number of bytes in block.
            /// </summary>
            public System.Int64 _ByteCount;

            public object ByteCount
            {
                get { return _ByteCount; }
                set { _ByteCount = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Carrier frequency of IR received.
            /// </summary>
            public System.Int64 _CarrierFrequency;

            public object CarrierFrequency
            {
                get { return _CarrierFrequency; }
                set { _CarrierFrequency = int.Parse(value.ToString()); }
            }
        }

        #endregion

        #region Nested type: StartReceiveParams

        private interface IStartReceiveParams
        {
            object Receiver { get; set; }
            object Timeout { get; set; }
        }

        /// <summary>
        /// Parameters for StartReceive.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct StartReceiveParams32 : IStartReceiveParams
        {
            /// <summary>
            /// Index of the receiver to use.
            /// </summary>
            public System.Int32 _Receiver;

            public object Receiver
            {
                get { return _Receiver; }
                set { _Receiver = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Receive timeout, in milliseconds.
            /// </summary>
            public System.Int32 _Timeout;

            public object Timeout
            {
                get { return _Timeout; }
                set { _Timeout = int.Parse(value.ToString()); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartReceiveParams64 : IStartReceiveParams
        {
            /// <summary>
            /// Index of the receiver to use.
            /// </summary>
            public System.Int64 _Receiver;

            public object Receiver
            {
                get { return _Receiver; }
                set { _Receiver = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Receive timeout, in milliseconds.
            /// </summary>
            public System.Int64 _Timeout;

            public object Timeout
            {
                get { return _Timeout; }
                set { _Timeout = int.Parse(value.ToString()); }
            }
        }

        #endregion

        #region Nested type: TransmitChunk

        private interface ITransmitChunk
        {
            object OffsetToNextChunk { get; set; }
            object RepeatCount { get; set; }
            object ByteCount { get; set; }
        }

        /// <summary>
        /// Information for transmitting IR.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct TransmitChunk32 : ITransmitChunk
        {
            /// <summary>
            /// Next chunk offset.
            /// </summary>
            public System.Int32 _OffsetToNextChunk;

            public object OffsetToNextChunk
            {
                get { return _OffsetToNextChunk; }
                set { _OffsetToNextChunk = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Repeat count.
            /// </summary>
            public System.Int32 _RepeatCount;

            public object RepeatCount
            {
                get { return _RepeatCount; }
                set { _RepeatCount = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Number of bytes.
            /// </summary>
            public System.Int32 _ByteCount;

            public object ByteCount
            {
                get { return _ByteCount; }
                set { _ByteCount = int.Parse(value.ToString()); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TransmitChunk64 : ITransmitChunk
        {
            /// <summary>
            /// Next chunk offset.
            /// </summary>
            public System.Int64 _OffsetToNextChunk;

            public object OffsetToNextChunk
            {
                get { return _OffsetToNextChunk; }
                set { _OffsetToNextChunk = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Repeat count.
            /// </summary>
            public System.Int64 _RepeatCount;

            public object RepeatCount
            {
                get { return _RepeatCount; }
                set { _RepeatCount = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Number of bytes.
            /// </summary>
            public System.Int64 _ByteCount;

            public object ByteCount
            {
                get { return _ByteCount; }
                set { _ByteCount = int.Parse(value.ToString()); }
            }
        }

        #endregion

        #region Nested type: TransmitParams

        private interface ITransmitParams
        {
            object TransmitPortMask { get; set; }
            object CarrierPeriod { get; set; }
            object Flags { get; set; }
            object PulseSize { get; set; }
        }

        /// <summary>
        /// Parameters for transmitting IR.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct TransmitParams32 : ITransmitParams
        {
            /// <summary>
            /// Bitmask containing ports to transmit on.
            /// </summary>
            public System.Int32 _TransmitPortMask;

            public object TransmitPortMask
            {
                get { return _TransmitPortMask; }
                set { _TransmitPortMask = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Carrier period.
            /// </summary>
            public System.Int32 _CarrierPeriod;

            public object CarrierPeriod
            {
                get { return _CarrierPeriod; }
                set { _CarrierPeriod = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Transmit Flags.
            /// </summary>
            public System.Int32 _Flags;

            public object Flags
            {
                get { return _Flags; }
                set { _Flags = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Pulse Size.  If Pulse Mode Flag set.
            /// </summary>
            public System.Int32 _PulseSize;

            public object PulseSize
            {
                get { return _PulseSize; }
                set { _PulseSize = int.Parse(value.ToString()); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TransmitParams64 : ITransmitParams
        {
            /// <summary>
            /// Bitmask containing ports to transmit on.
            /// </summary>
            public System.Int64 _TransmitPortMask;

            public object TransmitPortMask
            {
                get { return _TransmitPortMask; }
                set { _TransmitPortMask = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Carrier period.
            /// </summary>
            public System.Int64 _CarrierPeriod;

            public object CarrierPeriod
            {
                get { return _CarrierPeriod; }
                set { _CarrierPeriod = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Transmit Flags.
            /// </summary>
            public System.Int64 _Flags;

            public object Flags
            {
                get { return _Flags; }
                set { _Flags = int.Parse(value.ToString()); }
            }

            /// <summary>
            /// Pulse Size.  If Pulse Mode Flag set.
            /// </summary>
            public System.Int64 _PulseSize;

            public object PulseSize
            {
                get { return _PulseSize; }
                set { _PulseSize = int.Parse(value.ToString()); }
            }
        }

        #endregion

        #endregion Structures

        #region Enumerations

        //#region Nested type: DeviceCapabilityFlags

        ///// <summary>
        ///// IR Device Capability Flags.
        ///// </summary>
        //[Flags]
        //private enum DeviceCapabilityFlags
        //{
        //  /// <summary>
        //  /// Hardware supports legacy key signing.
        //  /// </summary>
        //  LegacySigning = 0x0001,
        //  /// <summary>
        //  /// Hardware has unique serial number.
        //  /// </summary>
        //  SerialNumber = 0x0002,
        //  /// <summary>
        //  /// Can hardware flash LED to identify receiver? 
        //  /// </summary>
        //  FlashLed = 0x0004,
        //  /// <summary>
        //  /// Is this a legacy device?
        //  /// </summary>
        //  Legacy = 0x0008,
        //  /// <summary>
        //  /// Device can wake from S1.
        //  /// </summary>
        //  WakeS1 = 0x0010,
        //  /// <summary>
        //  /// Device can wake from S2.
        //  /// </summary>
        //  WakeS2 = 0x0020,
        //  /// <summary>
        //  /// Device can wake from S3.
        //  /// </summary>
        //  WakeS3 = 0x0040,
        //  /// <summary>
        //  /// Device can wake from S4.
        //  /// </summary>
        //  WakeS4 = 0x0080,
        //  /// <summary>
        //  /// Device can wake from S5.
        //  /// </summary>
        //  WakeS5 = 0x0100,
        //}

        //#endregion

        #region Nested type: IoCtrl

        /// <summary>
        /// Device IO Control details.
        /// </summary>
        private enum IoCtrl
        {
            /// <summary>
            /// Start receiving IR.
            /// </summary>
            StartReceive = 0x0F608028,

            /// <summary>
            /// Stop receiving IR.
            /// </summary>
            StopReceive = 0x0F60802C,

            /// <summary>
            /// Get IR device details.
            /// </summary>
            GetDetails = 0x0F604004,

            /// <summary>
            /// Get IR blasters
            /// </summary>
            GetBlasters = 0x0F604008,

            /// <summary>
            /// Receive IR.
            /// </summary>
            Receive = 0x0F604022,

            /// <summary>
            /// Transmit IR.
            /// </summary>
            Transmit = 0x0F608015,
        }

        #endregion

        #region Nested type: ReadThreadMode

        /// <summary>
        /// Read Thread Mode.
        /// </summary>
        private enum ReadThreadMode
        {
            Receiving,
            Learning,
            LearningDone,
            LearningFailed,
            Stop,
        }

        #endregion

        #region Nested type: TransmitMode

        /// <summary>
        /// Used to set the carrier mode for IR blasting.
        /// </summary>
        internal enum TransmitMode
        {
            /// <summary>
            /// Carrier Mode.
            /// </summary>
            CarrierMode = 0,

            /// <summary>
            /// DC Mode.
            /// </summary>
            DCMode = 1,
        }

        #endregion

        #endregion Enumerations

        #region Constants

        private const int DeviceBufferSize = 100;
        private const int PacketTimeout = 100;
        //private const int WriteSyncTimeout = 10000;

        private const int ReadThreadTimeout = 200;
        private const int MaxReadThreadTries = 10;

        private const int ErrorBadCommand = 22;
        private const int ErrorOperationAborted = 995;

        #endregion Constants

        #region Variables

        #region Device Details

        private int _learnPort;
        private int _learnPortMask;
        private int _numTxPorts;

        private int _receivePort;
        private int _txPortMask;

        #endregion Device Details

        private bool _deviceAvailable;
        private SafeFileHandle _eHomeHandle;
        private IRCode _learningCode;

        private Thread _readThread;
        private ReadThreadMode _readThreadMode;
        private ReadThreadMode _readThreadModeNext;
        private bool _deviceReceiveStarted;

        private bool _isSystem64Bit;

        #endregion Variables

        #region Constructor

        public DriverVista(Guid deviceGuid, string devicePath)
            : base(deviceGuid, devicePath)
        {
        }

        #endregion Constructor

        #region Device Control Functions

        private void StartReceive(int receivePort, int timeout)
        {
            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            IStartReceiveParams structure;
            if (_isSystem64Bit)
            {
                structure = new StartReceiveParams64();
            }
            else
            {
                structure = new StartReceiveParams32();
            }
            structure.Receiver = receivePort;
            structure.Timeout = timeout;

            var structPtr = IntPtr.Zero;

            try
            {
                structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
                Marshal.StructureToPtr(structure, structPtr, false);

                int bytesReturned;
                IoControl(IoCtrl.StartReceive, structPtr, Marshal.SizeOf(structure), IntPtr.Zero, 0, out bytesReturned);
            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structPtr);
            }
        }

        private void StopReceive()
        {
            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            int bytesReturned;
            IoControl(IoCtrl.StopReceive, IntPtr.Zero, 0, IntPtr.Zero, 0, out bytesReturned);
        }

        private void GetDeviceCapabilities()
        {
            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            IDeviceCapabilities structure;
            if (_isSystem64Bit)
            {
                structure = new DeviceCapabilities64();
            }
            else
            {
                structure = new DeviceCapabilities32();
            }

            var structPtr = IntPtr.Zero;

            try
            {
                structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));

                Marshal.StructureToPtr(structure, structPtr, false);

                int bytesReturned;
                IoControl(IoCtrl.GetDetails, IntPtr.Zero, 0, structPtr, Marshal.SizeOf(structure), out bytesReturned);

                if (_isSystem64Bit)
                {
                    structure = (DeviceCapabilities64) Marshal.PtrToStructure(structPtr, structure.GetType());
                }
                else
                {
                    structure = (DeviceCapabilities32) Marshal.PtrToStructure(structPtr, structure.GetType());
                }
            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structPtr);
            }

            _numTxPorts = Int32.Parse(structure.TransmitPorts.ToString());
            _learnPortMask = Int32.Parse(structure.LearningMask.ToString());

            var receivePort = FirstLowBit(_learnPortMask);
            if (receivePort != -1)
                _receivePort = receivePort;

            var learnPort = FirstHighBit(_learnPortMask);
            _learnPort = learnPort != -1 ? learnPort : _receivePort;

            //DeviceCapabilityFlags flags = (DeviceCapabilityFlags)structure.DetailsFlags.ToInt32();
            //_legacyDevice = (int)(flags & DeviceCapabilityFlags.Legacy) != 0;
            //_canFlashLed = (int)(flags & DeviceCapabilityFlags.FlashLed) != 0;

            DebugWriteLine("Device Capabilities:");
            DebugWriteLine("NumTxPorts:     {0}", _numTxPorts);
            DebugWriteLine("NumRxPorts:     {0}", structure.ReceivePorts);
            DebugWriteLine("LearnPortMask:  {0}", _learnPortMask);
            DebugWriteLine("ReceivePort:    {0}", _receivePort);
            DebugWriteLine("LearnPort:      {0}", _learnPort);
            DebugWriteLine("DetailsFlags:   {0}", structure.DetailsFlags);
        }

        private void GetBlasters()
        {
            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            if (_numTxPorts <= 0)
                return;

            IAvailableBlasters structure;
            if (_isSystem64Bit)
            {
                structure = new AvailableBlasters64();
            }
            else
            {
                structure = new AvailableBlasters32();
            }

            var structPtr = IntPtr.Zero;

            try
            {
                structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));

                Marshal.StructureToPtr(structure, structPtr, false);

                int bytesReturned;
                IoControl(IoCtrl.GetBlasters, IntPtr.Zero, 0, structPtr, Marshal.SizeOf(structure), out bytesReturned);

                if (_isSystem64Bit)
                {
                    structure = (AvailableBlasters64) Marshal.PtrToStructure(structPtr, structure.GetType());
                }
                else
                {
                    structure = (AvailableBlasters32) Marshal.PtrToStructure(structPtr, structure.GetType());
                }

            }
            finally
            {
                if (structPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structPtr);
            }

            _txPortMask = Int32.Parse(structure.Blasters.ToString());

            DebugWriteLine("TxPortMask:     {0}", _txPortMask);
        }

        private void TransmitIR(byte[] irData, int carrier, int transmitPortMask)
        {
            DebugWriteLine("TransmitIR({0} bytes, carrier: {1}, port: {2})", irData.Length, carrier, transmitPortMask);

            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            ITransmitParams transmitParams;
            if (_isSystem64Bit)
            {
                transmitParams = new TransmitParams64();
            }
            else
            {
                transmitParams = new TransmitParams32();
            }

            transmitParams.TransmitPortMask = transmitPortMask;

            if (carrier == IRCode.CarrierFrequencyUnknown)
                carrier = IRCode.CarrierFrequencyDefault;

            var mode = GetTransmitMode(carrier);
            if (mode == TransmitMode.CarrierMode)
                transmitParams.CarrierPeriod = GetCarrierPeriod(carrier);
            else
                transmitParams.PulseSize = carrier;

            transmitParams.Flags = (int) mode;


            ITransmitChunk transmitChunk;
            if (_isSystem64Bit)
            {
                transmitChunk = new TransmitChunk64();
            }
            else
            {
                transmitChunk = new TransmitChunk32();
            }

            transmitChunk.OffsetToNextChunk = 0;
            transmitChunk.RepeatCount = 1;
            transmitChunk.ByteCount = irData.Length;

            var bufferSize = irData.Length + Marshal.SizeOf(transmitChunk) + 8;
            var buffer = new byte[bufferSize];

            var rawTransmitChunk = RawSerialize(transmitChunk);
            Array.Copy(rawTransmitChunk, buffer, rawTransmitChunk.Length);

            Array.Copy(irData, 0, buffer, rawTransmitChunk.Length, irData.Length);

            var structurePtr = IntPtr.Zero;
            var bufferPtr = IntPtr.Zero;

            try
            {
                structurePtr = Marshal.AllocHGlobal(Marshal.SizeOf(transmitParams));
                bufferPtr = Marshal.AllocHGlobal(buffer.Length);

                Marshal.StructureToPtr(transmitParams, structurePtr, true);

                Marshal.Copy(buffer, 0, bufferPtr, buffer.Length);

                int bytesReturned;
                IoControl(IoCtrl.Transmit, structurePtr, Marshal.SizeOf(transmitParams), bufferPtr, bufferSize,
                          out bytesReturned);
            }
            finally
            {
                if (structurePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structurePtr);

                if (bufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(bufferPtr);
            }

            // Force a delay between blasts (hopefully solves back-to-back blast errors) ...
            Thread.Sleep(PacketTimeout);
        }

        private void IoControl(IoCtrl ioControlCode, IntPtr inBuffer, int inBufferSize, IntPtr outBuffer,
                               int outBufferSize,
                               out int bytesReturned)
        {
            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            using (WaitHandle waitHandle = new ManualResetEvent(false))
            {
                SafeHandle safeWaitHandle = waitHandle.SafeWaitHandle;

                var success = false;
                safeWaitHandle.DangerousAddRef(ref success);
                if (!success)
                    throw new InvalidOperationException("Failed to initialize safe wait handle");

                try
                {
                    var dangerousWaitHandle = safeWaitHandle.DangerousGetHandle();

                    var overlapped = new DeviceIoOverlapped();
                    overlapped.ClearAndSetEvent(dangerousWaitHandle);

                    var deviceIoControl = DeviceIoControl(_eHomeHandle, ioControlCode, inBuffer, inBufferSize,
                                                          outBuffer,
                                                          outBufferSize, out bytesReturned, overlapped.Overlapped);

                    var lastError = Marshal.GetLastWin32Error();

                    if (deviceIoControl) return;

                    // Now also handles Operation Aborted and Bad Command errors.
                    switch (lastError)
                    {
                        case ErrorIoPending:
                            waitHandle.WaitOne();

                            var getOverlapped = GetOverlappedResult(_eHomeHandle, overlapped.Overlapped,
                                                                    out bytesReturned, false);
                            lastError = Marshal.GetLastWin32Error();

                            if (!getOverlapped)
                            {
                                if (lastError == ErrorBadCommand)
                                    goto case ErrorBadCommand;
                                if (lastError == ErrorOperationAborted)
                                    goto case ErrorOperationAborted;
                                throw new Win32Exception(lastError);
                            }
                            break;

                        case ErrorBadCommand:
                            if (Thread.CurrentThread == _readThread)
                                //Cause receive restart
                                _deviceReceiveStarted = false;
                            break;

                        case ErrorOperationAborted:
                            if (Thread.CurrentThread != _readThread)
                                throw new Win32Exception(lastError);

                            //Cause receive restart
                            _deviceReceiveStarted = false;
                            break;

                        default:
                            throw new Win32Exception(lastError);
                    }
                }
                catch
                {
                    DebugWriteLine("IoControl: something went bad with StructToPtr or the other way around");
                    if (_eHomeHandle != null)
                        CancelIo(_eHomeHandle);

                    throw;
                }
                finally
                {
                    safeWaitHandle.DangerousRelease();
                }
            }
        }

        #endregion Device Control Functions

        #region Driver overrides

        /// <summary>
        /// Start using the device.
        /// </summary>
        public override void Start()
        {
            try
            {
                DebugOpen("MicrosoftMceTransceiver_DriverVista.log");
                DebugWriteLine("Start()");
                DebugWriteLine("Device Guid: {0}", _deviceGuid);
                DebugWriteLine("Device Path: {0}", _devicePath);

                _isSystem64Bit = Utils.Windows.IsSystem64Bit();
                DebugWriteLine("Operating system arch is {0}", _isSystem64Bit ? "x64" : "x86");

                OpenDevice();
                InitializeDevice();

                StartReadThread(ReadThreadMode.Receiving);
            }
            catch
            {
                DebugClose();
                throw;
            }
        }

        /// <summary>
        /// Stop access to the device.
        /// </summary>
        public override void Stop()
        {
            DebugWriteLine("Stop()");

            try
            {
                StopReadThread();
                CloseDevice();
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());
                throw;
            }
            finally
            {
                DebugClose();
            }
        }

        /// <summary>
        /// Computer is entering standby, suspend device.
        /// </summary>
        public override void Suspend()
        {
            DebugWriteLine("Suspend()");
        }

        /// <summary>
        /// Computer is returning from standby, resume device.
        /// </summary>
        public override void Resume()
        {
            DebugWriteLine("Resume()");

            try
            {
                if (String.IsNullOrEmpty(Find(_deviceGuid)))
                {
                    DebugWriteLine("Device not found");
                    return;
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Learn an IR Command.
        /// </summary>
        /// <param name="learnTimeout">How long to wait before aborting learn.</param>
        /// <param name="learned">Newly learned IR Command.</param>
        /// <returns>Learn status.</returns>
        public override LearnStatus Learn(int learnTimeout, out IRCode learned)
        {
            DebugWriteLine("Learn()");

            RestartReadThread(ReadThreadMode.Learning);

            learned = null;
            _learningCode = new IRCode();

            var learnStartTick = Environment.TickCount;

            // Wait for the learning to finish ...
            while (_readThreadMode == ReadThreadMode.Learning && Environment.TickCount < learnStartTick + learnTimeout)
                Thread.Sleep(PacketTimeout);

            DebugWriteLine("End Learn");

            var modeWas = _readThreadMode;

            RestartReadThread(ReadThreadMode.Receiving);

            var status = LearnStatus.Failure;

            switch (modeWas)
            {
                case ReadThreadMode.Learning:
                    status = LearnStatus.Timeout;
                    break;

                case ReadThreadMode.LearningFailed:
                    status = LearnStatus.Failure;
                    break;

                case ReadThreadMode.LearningDone:
                    DebugDump(_learningCode.TimingData);
                    if (_learningCode.FinalizeData())
                    {
                        learned = _learningCode;
                        status = LearnStatus.Success;
                    }
                    break;
            }

            _learningCode = null;
            return status;
        }

        /// <summary>
        /// Send an IR Command.
        /// </summary>
        /// <param name="code">IR Command data to send.</param>
        /// <param name="port">IR port to send to.</param>
        public override void Send(IRCode code, int port)
        {
            DebugWrite("Send(): ");
            DebugDump(code.TimingData);

            var data = DataPacket(code);

            var portMask = 0;
            // Hardware ports map to bits in mask with Port 1 at left, ascending to right
            switch ((BlasterPort) port)
            {
                case BlasterPort.Both:
                    portMask = _txPortMask;
                    break;
                case BlasterPort.Port_1:
                    portMask = GetHighBit(_txPortMask, _numTxPorts);
                    break;
                case BlasterPort.Port_2:
                    portMask = GetHighBit(_txPortMask, _numTxPorts - 1);
                    break;
            }

            TransmitIR(data, code.Carrier, portMask);
        }

        #endregion Driver overrides

        #region Implementation

        /// <summary>
        /// Initializes the device.
        /// </summary>
        private void InitializeDevice()
        {
            DebugWriteLine("InitializeDevice()");

            GetDeviceCapabilities();
            GetBlasters();
        }

        /// <summary>
        /// Converts an IrCode into raw data for the device.
        /// </summary>
        /// <param name="code">IrCode to convert.</param>
        /// <returns>Raw device data.</returns>
        internal static byte[] DataPacket(IRCode code)
        {
            DebugWriteLine("DataPacket()");

            if (code.TimingData.Length == 0)
                return null;

            var data = new byte[code.TimingData.Length*4];

            var dataIndex = 0;
            foreach (var timing in code.TimingData)
            {
                var time = (uint) (50*(int) Math.Round((double) timing/50));

                for (var timeShift = 0; timeShift < 4; timeShift++)
                {
                    data[dataIndex++] = (byte) (time & 0xFF);
                    time >>= 8;
                }
            }

            return data;
        }

        /// <summary>
        /// Start the device read thread.
        /// </summary>
        private void StartReadThread(ReadThreadMode mode)
        {
            DebugWriteLine("StartReadThread({0})", Enum.GetName(typeof (ReadThreadMode), mode));

            if (_readThread != null)
            {
                DebugWriteLine("Read thread already started");
                return;
            }

            _deviceReceiveStarted = false;
            _readThreadModeNext = mode;

            _readThread = new Thread(ReadThread)
                              {
                                  Name = "MicrosoftMceTransceiver.DriverVista.ReadThread",
                                  IsBackground = true
                              };
            _readThread.Start();
        }

        /// <summary>
        /// Restart the device read thread.
        /// </summary>
        private void RestartReadThread(ReadThreadMode mode)
        {
            // Alternative to StopReadThread() ... StartReadThread(). Avoids Thread.Abort.

            _readThreadModeNext = mode;
            var numTriesLeft = MaxReadThreadTries;

            // Simple, optimistic wait for read thread to respond. Has room for improvement, but tends to work first time in practice.
            while (_readThreadMode != _readThreadModeNext && numTriesLeft-- != 0)
            {
                // Unblocks read thread, typically with Operation Aborted error. May cause Bad Command error in either thread.
                StopReceive();
                Thread.Sleep(ReadThreadTimeout);
            }

            if (numTriesLeft == 0)
                throw new InvalidOperationException("Failed to cycle read thread");
        }

        /// <summary>
        /// Stop the device read thread.
        /// </summary>
        private void StopReadThread()
        {
            DebugWriteLine("StopReadThread()");

            if (_readThread == null)
            {
                DebugWriteLine("Read thread already stopped");
                return;
            }

            if (_readThread.IsAlive)
            {
                _readThread.Abort();

                if (Thread.CurrentThread != _readThread)
                    _readThread.Join();
            }

            _readThreadMode = ReadThreadMode.Stop;

            _readThread = null;
        }

        /// <summary>
        /// Opens the device handles and registers for device removal notification.
        /// </summary>
        private void OpenDevice()
        {
            DebugWriteLine("OpenDevice()");

            if (_eHomeHandle != null)
            {
                DebugWriteLine("Device already open");
                return;
            }

            _eHomeHandle = CreateFile(_devicePath,
                                      CreateFileAccessTypes.GenericRead | CreateFileAccessTypes.GenericWrite,
                                      CreateFileShares.None, IntPtr.Zero, CreateFileDisposition.OpenExisting,
                                      CreateFileAttributes.Overlapped, IntPtr.Zero);
            var lastError = Marshal.GetLastWin32Error();
            if (_eHomeHandle.IsInvalid)
            {
                _eHomeHandle = null;
                throw new Win32Exception(lastError);
            }

            var success = false;
            _eHomeHandle.DangerousAddRef(ref success);
            if (success)
            {
            }
            else
            {
                DebugWriteLine("Warning: Failed to initialize device removal notification");
            }

            Thread.Sleep(PacketTimeout);
            // Hopefully improves compatibility with Zalman remote which times out retrieving device capabilities. (2008-01-01)

            _deviceAvailable = true;
        }

        /// <summary>
        /// Close all handles to the device and unregisters device removal notification.
        /// </summary>
        private void CloseDevice()
        {
            DebugWriteLine("CloseDevice()");

            _deviceAvailable = false;

            if (_eHomeHandle == null)
            {
                DebugWriteLine("Device already closed");
                return;
            }

            _eHomeHandle.DangerousRelease();

            _eHomeHandle.Dispose();
            _eHomeHandle = null;
        }

        /// <summary>
        /// Device read thread method.
        /// </summary>
        private void ReadThread()
        {
            var receiveParamsPtr = IntPtr.Zero;

            try
            {
                IReceiveParams receiveParams;
                if (_isSystem64Bit)
                {
                    receiveParams = new ReceiveParams64();
                }
                else
                {
                    receiveParams = new ReceiveParams32();
                }

                var receiveParamsSize = Marshal.SizeOf(receiveParams) + DeviceBufferSize + 8;
                receiveParamsPtr = Marshal.AllocHGlobal(receiveParamsSize);

                receiveParams.ByteCount = DeviceBufferSize;
                Marshal.StructureToPtr(receiveParams, receiveParamsPtr, false);

                while (_readThreadMode != ReadThreadMode.Stop)
                {
                    // Cycle thread if device stopped reading.
                    if (!_deviceReceiveStarted)
                    {
                        StartReceive(_readThreadModeNext == ReadThreadMode.Receiving ? _receivePort : _learnPort,
                                     PacketTimeout);
                        _readThreadMode = _readThreadModeNext;
                        _deviceReceiveStarted = true;
                    }

                    int bytesRead;
                    IoControl(IoCtrl.Receive, IntPtr.Zero, 0, receiveParamsPtr, receiveParamsSize, out bytesRead);

                    if (bytesRead > Marshal.SizeOf(receiveParams))
                    {
                        var dataSize = bytesRead;

                        bytesRead -= Marshal.SizeOf(receiveParams);

                        var packetBytes = new byte[bytesRead];
                        var dataBytes = new byte[dataSize];

                        Marshal.Copy(receiveParamsPtr, dataBytes, 0, dataSize);
                        Array.Copy(dataBytes, dataSize - bytesRead, packetBytes, 0, bytesRead);

                        var timingData = GetTimingDataFromPacket(packetBytes);

                        DebugWrite("{0:yyyy-MM-dd HH:mm:ss.ffffff} - ", DateTime.Now);
                        DebugWrite("Received timing:    ");
                        DebugDump(timingData);

                        if (_readThreadMode == ReadThreadMode.Learning)
                            _learningCode.AddTimingData(timingData);
                        //else
                        //  IrDecoder.DecodeIR(timingData, _remoteCallback, _keyboardCallback, _mouseCallback);
                    }

                    // Determine carrier frequency when learning ...
                    DebugWriteLine("bytesRead: {0}, receiveParams Size: {1}", bytesRead, Marshal.SizeOf(receiveParams));
                    if (_readThreadMode == ReadThreadMode.Learning && bytesRead >= Marshal.SizeOf(receiveParams))
                    {
                        IReceiveParams receiveParams2;
                        if (_isSystem64Bit)
                        {
                            receiveParams2 =
                                (ReceiveParams64) Marshal.PtrToStructure(receiveParamsPtr, receiveParams.GetType());
                        }
                        else
                        {
                            receiveParams2 =
                                (ReceiveParams32) Marshal.PtrToStructure(receiveParamsPtr, receiveParams.GetType());
                        }
                        DebugWriteLine("DataEnd {0}", Convert.ToInt64(receiveParams2.DataEnd));
                        if (Convert.ToInt64(receiveParams2.DataEnd) != 0)
                        {
                            _learningCode.Carrier = Convert.ToInt32(receiveParams2.CarrierFrequency);
                            _readThreadMode = ReadThreadMode.LearningDone;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());

                if (_eHomeHandle != null)
                    CancelIo(_eHomeHandle);
            }
            finally
            {
                if (receiveParamsPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(receiveParamsPtr);

                try
                {
                    if (_eHomeHandle != null)
                        StopReceive();
                }
                catch (Exception ex)
                {
                    DebugWriteLine(ex.ToString());
                }
            }

            DebugWriteLine("Read Thread Ended");
        }

        #endregion Implementation

        #region Misc Methods

        internal static byte[] RawSerialize(object anything)
        {
            var rawSize = Marshal.SizeOf(anything);
            var rawData = new byte[rawSize];

            var handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);

            try
            {
                var buffer = handle.AddrOfPinnedObject();

                Marshal.StructureToPtr(anything, buffer, false);
            }
            finally
            {
                handle.Free();
            }

            return rawData;
        }

        internal static int GetHighBit(int mask, int bitCount)
        {
            var count = 0;
            for (var i = 0; i < 32; i++)
            {
                var bitMask = 1 << i;

                if ((mask & bitMask) != 0)
                    if (++count == bitCount)
                        return bitMask;
            }

            return 0;
        }

        internal static int FirstHighBit(int mask)
        {
            for (var i = 0; i < 32; i++)
                if ((mask & (1 << i)) != 0)
                    return i;

            return -1;
        }

        internal static int FirstLowBit(int mask)
        {
            for (var i = 0; i < 32; i++)
                if ((mask & (1 << i)) == 0)
                    return i;

            return -1;
        }

        internal static int GetCarrierPeriod(int carrier)
        {
            return (int) Math.Round(1000000.0/carrier);
        }

        internal static TransmitMode GetTransmitMode(int carrier)
        {
            return carrier > 100 ? TransmitMode.CarrierMode : TransmitMode.DCMode;
        }

        internal static int[] GetTimingDataFromPacket(byte[] packetBytes)
        {
            var timingData = new int[packetBytes.Length/4];

            var timingDataIndex = 0;

            for (var index = 0; index < packetBytes.Length; index += 4)
                timingData[timingDataIndex++] =
                    (packetBytes[index] +
                     (packetBytes[index + 1] << 8) +
                     (packetBytes[index + 2] << 16) +
                     (packetBytes[index + 3] << 24));

            return timingData;
        }

        #endregion Misc Methods
    }
}
