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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using PyMCE_Core.Infrared;

namespace PyMCE_Core.Device
{
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

        #endregion

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

        #region Nested type: ReadingMode

        /// <summary>
        /// Reading Mode.
        /// </summary>
        private enum ReadingMode
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
        private enum TransmitMode
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

        #endregion

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

        #region Device

        private SafeFileHandle _deviceHandle;
        private bool _deviceAvailable;

        private int _deviceNumTxPorts;
        private int _deviceNumRxPorts;

        private int _deviceLearningMask;

        private int _deviceReceivePort;
        private bool _deviceReceiveStarted;

        private int _deviceLearnPort;

        private int _deviceTxPortMask;

        #endregion

        private IRCode _learningCode;

        private Thread _readingThread;
        private ReadingMode _readingThreadMode;
        private ReadingMode _readingThreadModeNext;

        private bool _isSystem64Bit;

        #endregion

        #region Constructor

        public DriverVista(Guid deviceGuid, string devicePath)
            : base(deviceGuid, devicePath)
        {

        }

        #endregion

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
            if(!_deviceAvailable)
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
                structure = new DeviceCapabilities64();
            else
                structure = new DeviceCapabilities32();

            var structPtr = IntPtr.Zero;

            try
            {
                structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));

                Marshal.StructureToPtr(structure, structPtr, false);

                int bytesReturned;
                IoControl(IoCtrl.GetDetails, IntPtr.Zero, 0, structPtr, Marshal.SizeOf(structure),
                          out bytesReturned);

                if (_isSystem64Bit)
                    structure = (DeviceCapabilities64) Marshal.PtrToStructure(structPtr, structure.GetType());
                else
                    structure = (DeviceCapabilities32) Marshal.PtrToStructure(structPtr, structure.GetType());
            }
            finally
            {
                if(structPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structPtr);
            }

            _deviceNumTxPorts = Int32.Parse(structure.TransmitPorts.ToString());
            _deviceNumRxPorts = Int32.Parse(structure.ReceivePorts.ToString());
            _deviceLearningMask = Int32.Parse(structure.LearningMask.ToString());

            var receivePort = FirstLowBit(_deviceLearningMask);
            if (receivePort != -1)
                _deviceReceivePort = receivePort;

            var learnPort = FirstHighBit(_deviceLearningMask);
            _deviceLearnPort = learnPort != -1 ? learnPort : _deviceReceivePort;

            Debug.WriteLine("Device Capabilities:");
            Debug.WriteLine(string.Format("NumTxPorts:      {0}", _deviceNumTxPorts));
            Debug.WriteLine(string.Format("NumRxPorts:      {0}", _deviceNumRxPorts));
            Debug.WriteLine(string.Format("LearningMask:    {0}", _deviceLearningMask));
            Debug.WriteLine(string.Format("ReceivePort:     {0}", _deviceReceivePort));
            Debug.WriteLine(string.Format("LearnPort:       {0}", _deviceLearnPort));
            Debug.WriteLine(string.Format("DetailsFlags:    {0}", structure.DetailsFlags));

        }

        private void GetBlasters()
        {
            if(!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            if (_deviceNumTxPorts <= 0)
                return;

            IAvailableBlasters structure;
            if(_isSystem64Bit)
                structure = new AvailableBlasters64();
            else
                structure = new AvailableBlasters32();

            var structPtr = IntPtr.Zero;

            try
            {
                structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));

                Marshal.StructureToPtr(structure, structPtr, false);

                int bytesReturned;
                IoControl(IoCtrl.GetBlasters, IntPtr.Zero, 0, structPtr, Marshal.SizeOf(structure), out bytesReturned);

                if (_isSystem64Bit)
                    structure = (AvailableBlasters64) Marshal.PtrToStructure(structPtr, structure.GetType());
                else
                    structure = (AvailableBlasters32) Marshal.PtrToStructure(structPtr, structure.GetType());
            }
            finally
            {
                if(structPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structPtr);
            }

            _deviceTxPortMask = Int32.Parse(structure.Blasters.ToString());

            Debug.WriteLine(string.Format("TxPortMask:      {0}", _deviceTxPortMask));
        }

        private void TransmitIR(byte[] irData, int carrier, int transmitPortMask)
        {
            Debug.WriteLine(string.Format("TransmitIR({0} bytes, carrier: {1}, port: {2}", irData.Length, carrier, transmitPortMask));

            if(!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            ITransmitParams transmitParams;
            if(_isSystem64Bit)
                transmitParams = new TransmitParams64();
            else
                transmitParams = new TransmitParams32();

            transmitParams.TransmitPortMask = transmitPortMask;

            if (carrier == IRCode.CarrierFrequencyUnknown)
                carrier = IRCode.CarrierFrequencyDefault;

            TransmitMode mode = GetTransmitMode(carrier);
            if (mode == TransmitMode.CarrierMode)
                transmitParams.CarrierPeriod = GetCarrierPeriod(carrier);
            else
                transmitParams.PulseSize = carrier;

            transmitParams.Flags = (int) mode;

            ITransmitChunk transmitChunk;
            if(_isSystem64Bit)
                transmitChunk = new TransmitChunk64();
            else
                transmitChunk = new TransmitChunk32();

            transmitChunk.OffsetToNextChunk = 0;
            transmitChunk.RepeatCount = 1;
            transmitChunk.ByteCount = irData.Length;

            var bufferSize = irData.Length + Marshal.SizeOf(transmitChunk) + 8;
            var buffer = new byte[bufferSize];

            var rawTransmitChunk = RawSerialize(transmitChunk);
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
                if(structurePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(structurePtr);

                if(bufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(bufferPtr);
            }

            // Force a delay between blasts (hopefully solves back-to-back blast errors) ...
            Thread.Sleep(PacketTimeout);
        }

        private void IoControl(IoCtrl ioControlCode, IntPtr inBuffer, int inBufferSize, IntPtr outBuffer,
                               int outBufferSize, out int bytesReturned)
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

                    var deviceIoControl = DeviceIoControl(_deviceHandle, ioControlCode, inBuffer, inBufferSize,
                                                          outBuffer,
                                                          outBufferSize, out bytesReturned, overlapped.Overlapped);
                    var lastError = Marshal.GetLastWin32Error();

                    if (deviceIoControl) return;

                    // Now also handles Operation Aborted and Bad Command errors.
                    switch (lastError)
                    {
                        case ErrorIoPending:
                            waitHandle.WaitOne();

                            var getOverlapped = GetOverlappedResult(_deviceHandle, overlapped.Overlapped,
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
                            if (Thread.CurrentThread == _readingThread)
                                //Cause receive restart
                                _deviceReceiveStarted = false;
                            break;

                        case ErrorOperationAborted:
                            if (Thread.CurrentThread != _readingThread)
                                throw new Win32Exception(lastError);

                            //Cause receive restart
                            _deviceReceiveStarted = false;
                            break;

                        default:
                            throw new Win32Exception(lastError);
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("IoControl: something went bad with StructToPtr or the other way around");
                    if (_deviceHandle != null)
                        CancelIo(_deviceHandle);

                    throw;
                }
                finally
                {
                    safeWaitHandle.DangerousRelease();
                }
            }
        }

        #endregion

        #region Driver overrides

        public override void Start()
        {
            Debug.WriteLine("Start()");
            Debug.WriteLine(string.Format("Device Guid: {0}", DeviceGuid));
            Debug.WriteLine(string.Format("Device Path: {0}", DevicePath));

            _isSystem64Bit = Utils.Windows.IsSystem64Bit();
            Debug.WriteLine(string.Format("Operating system arch is {0}", _isSystem64Bit ? "x64" : "x86"));

            _deviceAvailable = OpenDevice();
            if (!_deviceAvailable) return;

            InitializeDevice();

            StartReading(ReadingMode.Receiving);
        }

        public override void Stop()
        {
            Debug.WriteLine("Stop()");

            try
            {
                StopReading();
                CloseDevice();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        public override void Suspend()
        {
            Debug.WriteLine("Suspend()");
        }

        public override void Resume()
        {
            Debug.WriteLine("Resume()");

            try
            {
                if(String.IsNullOrEmpty(Find(DeviceGuid)))
                {
                    Debug.WriteLine("Device not found");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        public override Transceiver.LearnStatus Learn(int learnTimeout, out IRCode learned)
        {
            Debug.WriteLine("Learn()");

            RestartReading(ReadingMode.Learning);

            learned = null;
            _learningCode = new IRCode();

            var learnStartTick = Environment.TickCount;

            // Wait for the learning to finish ...
            while(_readingThreadMode == ReadingMode.Learning && Environment.TickCount < learnStartTick + learnTimeout)
                Thread.Sleep(PacketTimeout);

            Debug.WriteLine("End learn");

            var modeWas = _readingThreadMode;

            RestartReading(ReadingMode.Receiving);

            var status = Transceiver.LearnStatus.Failure;

            switch (modeWas)
            {
                case ReadingMode.Learning:
                    status = Transceiver.LearnStatus.Timeout;
                    break;

                case ReadingMode.LearningFailed:
                    status = Transceiver.LearnStatus.Failure;
                    break;

                case ReadingMode.LearningDone:
                    //DebugDump(_learningCode.TimingData);
                    if (_learningCode.FinalizeData())
                    {
                        learned = _learningCode;
                        status = Transceiver.LearnStatus.Success;
                    }
                    break;
            }

            _learningCode = null;
            return status;
        }

        public override void Send(IRCode code, int port)
        {
            Debug.WriteLine("Send()");
            //DebugDump(code.TimingData);

            var data = DataPacket(code);

            // Hardware ports map to bits in mask with Port 1 at left, ascending to right
            var portMask = 0;
            switch ((Transceiver.BlasterPort) port)
            {
                case Transceiver.BlasterPort.Both:
                    portMask = _deviceTxPortMask;
                    break;
                case Transceiver.BlasterPort.Port_1:
                    portMask = GetHighBit(_deviceTxPortMask, _deviceNumTxPorts);
                    break;
                case Transceiver.BlasterPort.Port_2:
                    portMask = GetHighBit(_deviceTxPortMask, _deviceNumTxPorts - 1);
                    break;
            }

            TransmitIR(data, code.Carrier, portMask);
        }

        #endregion

        #region Implementation

        /// <summary>
        /// Converts an IrCode into raw data for the device.
        /// </summary>
        /// <param name="code">IrCode to convert.</param>
        /// <returns>Raw device data.</returns>
        private static byte[] DataPacket(IRCode code)
        {
            Debug.WriteLine("DataPacket()");

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

        #region Device Methods

        /// <summary>
        /// Opens the device handles and registers for device removal notification.
        /// </summary>
        private bool OpenDevice()
        {
            Debug.WriteLine("OpenDevice()");

            if (_deviceHandle != null)
            {
                Debug.WriteLine("Device already open");
                return false;
            }

            _deviceHandle = CreateFile(DevicePath,
                                       CreateFileAccessTypes.GenericRead | CreateFileAccessTypes.GenericWrite,
                                       CreateFileShares.None, IntPtr.Zero, CreateFileDisposition.OpenExisting,
                                       CreateFileAttributes.Overlapped, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();
            if (_deviceHandle.IsInvalid)
            {
                _deviceHandle = null;
                throw new Win32Exception(lastError);
            }

            var success = false;
            _deviceHandle.DangerousAddRef(ref success);
            if (!success)
            {
                Debug.WriteLine("Warning: Failed to initialize device removal notification");
            }

            Thread.Sleep(PacketTimeout);

            return true;
        }

        private void CloseDevice()
        {
            Debug.WriteLine("CloseDevice()");

            _deviceAvailable = false;

            if(_deviceHandle == null)
            {
                Debug.WriteLine("Device already closed");
                return;
            }

            _deviceHandle.DangerousRelease();

            _deviceHandle.Dispose();
            _deviceHandle = null;
        }

        private void InitializeDevice()
        {
            Debug.WriteLine("InitiailizeDevice()");

            GetDeviceCapabilities();
            GetBlasters();
        }

        #endregion

        #region Reading Control Methods

        private void StartReading(ReadingMode mode)
        {
            if(_readingThread != null)
            {
                Debug.WriteLine("Reading already started");
                return;
            }

            _deviceReceiveStarted = false;
            _readingThreadModeNext = mode;

            _readingThread = new Thread(ReadingThread)
                                 {
                                     Name = "PyMCE.DriverVista.ReadingThread",
                                     IsBackground = true
                                 };
            _readingThread.Start();
        }

        /// <summary>
        /// Restart the device read thread.
        /// </summary>
        private void RestartReading(ReadingMode mode)
        {
            // Alternative to StopReadThread() ... StartReadThread(). Avoids Thread.Abort.

            _readingThreadModeNext = mode;
            var numTriesLeft = MaxReadThreadTries;

            // Simple, optimistic wait for read thread to respond. Has room for improvement, but tends to work first time in practice.
            while (_readingThreadMode != _readingThreadModeNext && numTriesLeft-- != 0)
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
        private void StopReading()
        {
            Debug.WriteLine("StopReading()");

            if (_readingThread == null)
            {
                Debug.WriteLine("Read thread already stopped");
                return;
            }

            //if (_eHomeHandle != null)
            //  CancelIo(_eHomeHandle);

            if (_readingThread.IsAlive)
            {
                _readingThread.Abort();

                if (Thread.CurrentThread != _readingThread)
                    _readingThread.Join();
            }

            _readingThreadMode = ReadingMode.Stop;

            _readingThread = null;
        }

        #endregion

        private void ReadingThread()
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

                int receiveParamsSize = Marshal.SizeOf(receiveParams) + DeviceBufferSize + 8;
                receiveParamsPtr = Marshal.AllocHGlobal(receiveParamsSize);

                receiveParams.ByteCount = DeviceBufferSize;
                Marshal.StructureToPtr(receiveParams, receiveParamsPtr, false);

                while (_readingThreadMode != ReadingMode.Stop)
                {
                    // Cycle thread if device stopped reading.
                    if (!_deviceReceiveStarted)
                    {
                        StartReceive(
                            _readingThreadModeNext == ReadingMode.Receiving ? _deviceReceivePort : _deviceLearnPort,
                            PacketTimeout);

                        _readingThreadMode = _readingThreadModeNext;
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

                        int[] timingData = GetTimingDataFromPacket(packetBytes);

                        Debug.Write(string.Format("{0:yyyy-MM-dd HH:mm:ss.ffffff} - ", DateTime.Now));
                        Debug.WriteLine("Received timing:    ");
                        DebugDump(timingData);

                        if (_readingThreadMode == ReadingMode.Learning)
                            _learningCode.AddTimingData(timingData);
                        else
                            Console.WriteLine(string.Format("timingData.Length: {0}", timingData.Length));
                            //IrDecoder.DecodeIR(timingData, _remoteCallback, _keyboardCallback, _mouseCallback);
                    }

                    // Determine carrier frequency when learning ...
                    if (_readingThreadMode == ReadingMode.Learning && bytesRead >= Marshal.SizeOf(receiveParams))
                    {
                        IReceiveParams receiveParams2;
                        if (_isSystem64Bit)
                        {
                            receiveParams2 = (ReceiveParams64)Marshal.PtrToStructure(receiveParamsPtr, receiveParams.GetType());
                        }
                        else
                        {
                            receiveParams2 = (ReceiveParams32)Marshal.PtrToStructure(receiveParamsPtr, receiveParams.GetType());
                        }
                        if (System.Convert.ToInt64(receiveParams2.DataEnd) != 0)
                        {
                            _learningCode.Carrier = System.Convert.ToInt32(receiveParams2.CarrierFrequency);
                            _readingThreadMode = ReadingMode.LearningDone;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                if (_deviceHandle != null)
                    CancelIo(_deviceHandle);
            }
            finally
            {
                if (receiveParamsPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(receiveParamsPtr);

                try
                {
                    if (_deviceHandle != null)
                        StopReceive();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            Debug.WriteLine("Read Thread Ended");
        }

        #endregion

        #region Misc Methods

        private static byte[] RawSerialize(object anything)
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

        private static int GetHighBit(int mask, int bitCount)
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

        private static int FirstHighBit(int mask)
        {
            for (int i = 0; i < 32; i++)
                if ((mask & (1 << i)) != 0)
                    return i;

            return -1;
        }

        private static int FirstLowBit(int mask)
        {
            for (int i = 0; i < 32; i++)
                if ((mask & (1 << i)) == 0)
                    return i;

            return -1;
        }

        private static int GetCarrierPeriod(int carrier)
        {
            return (int)Math.Round(1000000.0 / carrier);
        }

        private static TransmitMode GetTransmitMode(int carrier)
        {
            return carrier > 100 ? TransmitMode.CarrierMode : TransmitMode.DCMode;
        }

        private static int[] GetTimingDataFromPacket(byte[] packetBytes)
        {
            var timingData = new int[packetBytes.Length / 4];

            var timingDataIndex = 0;

            for (var index = 0; index < packetBytes.Length; index += 4)
                timingData[timingDataIndex++] =
                  (packetBytes[index] +
                   (packetBytes[index + 1] << 8) +
                   (packetBytes[index + 2] << 16) +
                   (packetBytes[index + 3] << 24));

            return timingData;
        }

        #endregion
    }
}
