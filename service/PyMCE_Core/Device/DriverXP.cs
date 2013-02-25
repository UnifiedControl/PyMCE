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

using Microsoft.Win32.SafeHandles;
using PyMCE.Core.Infrared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace PyMCE.Core.Device
{
    /// <summary>
    /// Driver class for the Windows XP eHome driver.
    /// </summary>
    internal class DriverXP : Driver
    {
        #region Interop

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadFile(
          SafeFileHandle handle,
          IntPtr buffer,
          int bytesToRead,
          out int bytesRead,
          IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteFile(
          SafeFileHandle handle,
          byte[] buffer,
          int bytesToWrite,
          out int bytesWritten,
          IntPtr overlapped);

        #endregion Interop

        #region Enumerations

        #region Nested type: DeviceType

        /// <summary>
        /// Type of device in use.
        /// This is used to determine the blaster port selection method.
        /// </summary>
        private enum DeviceType
        {
            /// <summary>
            /// Device is a first party Microsoft MCE transceiver.
            /// </summary>
            Microsoft = 0,
            /// <summary>
            /// Device is a third party SMK or Topseed MCE transceiver.
            /// </summary>
            SmkTopseed = 1,
        }

        #endregion

        #region Nested type: InputPort

        /// <summary>
        /// Device input port.
        /// </summary>
        private enum InputPort
        {
            Receive = 0,
            Learning = 1,
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

        #endregion Enumerations

        #region Constants

        private const int DeviceBufferSize = 100;
        private const int PacketTimeout = 100;
        private const int TimingResolution = 50; // In microseconds.

        // Vendor ID's for SMK and Topseed devices.
        private const string VidSMK = "vid_1784";
        private const string VidTopseed = "vid_0609";

        // Device variables
        private const int WriteSyncTimeout = 10000;

        // Microsoft Port Packets
        private static readonly byte[][] MicrosoftPorts = new byte[][]
                                                        {
                                                          new byte[] {0x9F, 0x08, 0x06}, // Both
                                                          new byte[] {0x9F, 0x08, 0x04}, // 1
                                                          new byte[] {0x9F, 0x08, 0x02}, // 2
                                                        };

        // SMK / Topseed Port Packets

        //static readonly byte[] ResetPacket = { 0xFF, 0xFE };

        // Misc Packets
        private static readonly byte[] SetCarrierFreqPacket = { 0x9F, 0x06, 0x01, 0x80 };
        private static readonly byte[] SetInputPortPacket = { 0x9F, 0x14, 0x00 };
        private static readonly byte[] SetTimeoutPacket = { 0x9F, 0x0C, 0x00, 0x00 };

        private static readonly byte[][] SmkTopseedPorts = new byte[][]
                                                         {
                                                           new byte[] {0x9F, 0x08, 0x00}, // Both
                                                           new byte[] {0x9F, 0x08, 0x01}, // 1
                                                           new byte[] {0x9F, 0x08, 0x02}, // 2
                                                         };

        // Start, Stop and Reset Packets
        private static readonly byte[] StartPacket = { 0x00, 0xFF, 0xAA };

        private static readonly byte[] StopPacket = {
                                                  0xFF, 0xBB,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                                                  0xFF, 0xFF
                                                };

        #endregion Constants

        #region Variables

        private readonly DeviceType _deviceType = DeviceType.Microsoft;
        private int _decodeCarry;
        private bool _deviceAvailable;
        private SafeFileHandle _eHomeHandle;
        private IRCode _learningCode;

        private Thread _readThread;
        private ReadThreadMode _readThreadMode;
        private ManualResetEvent _stopReadThread;

        #endregion Variables

        #region Constructor

        public DriverXP(Guid deviceGuid, string devicePath)
            : base(deviceGuid, devicePath)
        {
            if (devicePath.IndexOf(VidSMK, StringComparison.OrdinalIgnoreCase) != -1 ||
                devicePath.IndexOf(VidTopseed, StringComparison.OrdinalIgnoreCase) != -1)
                _deviceType = DeviceType.SmkTopseed;
            else
                _deviceType = DeviceType.Microsoft;
        }

        #endregion Constructor

        #region Driver overrides

        /// <summary>
        /// Start using the device.
        /// </summary>
        public override void Start()
        {
            try
            {
                DebugOpen("MicrosoftMceTransceiver_DriverXP.log");
                DebugWriteLine("Start()");
                DebugWriteLine("Device Guid: {0}", _deviceGuid);
                DebugWriteLine("Device Path: {0}", _devicePath);
                DebugWriteLine("Device Type: {0}", Enum.GetName(typeof(DeviceType), _deviceType));

                FireStateChanged(new StateChangedEventArgs(RunningState.Starting));

                OpenDevice();
                StartReadThread();
                InitializeDevice();
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

            FireStateChanged(new StateChangedEventArgs(RunningState.Stopping));

            try
            {
                if (_eHomeHandle == null)
                    throw new InvalidOperationException("Cannot stop, device is not active");

                WriteSync(StopPacket);
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
                FireStateChanged(new StateChangedEventArgs(RunningState.Stopped));
            }
        }

        /// <summary>
        /// Computer is entering standby, suspend device.
        /// </summary>
        public override void Suspend()
        {
            DebugWriteLine("Suspend()");

            try
            {
                if (_eHomeHandle == null)
                {
                    DebugWriteLine("Warning: Device is not active");
                    return;
                }

                WriteSync(StopPacket);

                StopReadThread();
                CloseDevice();
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());
                throw;
            }
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

                if (_eHomeHandle != null)
                    throw new InvalidOperationException("Cannot resume, device is already active");

                //_notifyWindow.RegisterDeviceArrival();

                OpenDevice();
                StartReadThread();
                InitializeDevice();
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

            learned = null;
            _learningCode = new IRCode();

            SetInputPort(InputPort.Learning);

            var learnStartTick = Environment.TickCount;
            _readThreadMode = ReadThreadMode.Learning;

            // Wait for the learning to finish ...
            while (_readThreadMode == ReadThreadMode.Learning && Environment.TickCount < learnStartTick + learnTimeout)
                Thread.Sleep(PacketTimeout);

            DebugWriteLine("End Learn");

            var modeWas = _readThreadMode;

            _readThreadMode = ReadThreadMode.Receiving;
            SetInputPort(InputPort.Receive);

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

            // Reset device (hopefully this will stop the blaster from stalling)
            //WriteSync(ResetPacket);

            SetOutputPort(port);
            SetCarrierFrequency(code.Carrier);

            // Send packet
            var data = DataPacket(code);

            // If the code would go longer than the allowable limit, truncate the code
            if (data.Length > 341)
            // 340 minus the overhead of 1 byte in 4 for header is 255 bytes of actual time code data, add one for a terminator byte
            {
                Array.Resize(ref data, 341); // Shrink the array to fit
                data[340] = 0x80; // Set the terminator byte
            }

            WriteSync(data);
        }

        #endregion Driver overrides

        #region Implementation

        /// <summary>
        /// Initializes the device.
        /// </summary>
        private void InitializeDevice()
        {
            DebugWriteLine("InitializeDevice()");

            WriteSync(StartPacket);

            // Testing some commands that MCE sends, but I don't know what they mean (what do these get back?)
            WriteSync(new byte[] { 0xFF, 0x0B }); // Looks like a request for Firmware version
            WriteSync(new byte[] { 0x9F, 0x05 });
            WriteSync(new byte[] { 0x9F, 0x0D });
            WriteSync(new byte[] { 0x9F, 0x13 });

            Thread.Sleep(4 * PacketTimeout);

            SetTimeout(PacketTimeout);
            SetInputPort(InputPort.Receive);
        }

        /// <summary>
        /// Converts an IrCode into raw data for the device.
        /// </summary>
        /// <param name="code">IrCode to convert.</param>
        /// <returns>Raw device data.</returns>
        private static byte[] DataPacket(IRCode code)
        {
            DebugWriteLine("DataPacket()");

            if (code.TimingData.Length == 0)
                return null;

            // Construct data bytes into "packet" ...
            var packet = new List<byte>();

            for (int index = 0; index < code.TimingData.Length; index++)
            {
                double time = code.TimingData[index];

                var duration = (byte)Math.Abs(Math.Round(time / TimingResolution));
                var pulse = (time > 0);

                DebugWrite("{0}{1}, ", pulse ? '+' : '-', duration * TimingResolution);

                while (duration > 0x7F)
                {
                    packet.Add((byte)(pulse ? 0xFF : 0x7F));

                    duration -= 0x7F;
                }

                packet.Add((byte)(pulse ? 0x80 | duration : duration));
            }

            DebugWriteNewLine();

            // Insert byte count markers into packet data bytes ...
            var subpackets = (int)Math.Ceiling(packet.Count / (double)4);

            var output = new byte[packet.Count + subpackets + 1];

            var outputPos = 0;

            for (var packetPos = 0; packetPos < packet.Count; )
            {
                var copyCount = (byte)(packet.Count - packetPos < 4 ? packet.Count - packetPos : 0x04);

                output[outputPos++] = (byte)(0x80 | copyCount);

                for (var index = 0; index < copyCount; index++)
                    output[outputPos++] = packet[packetPos++];
            }

            output[outputPos] = 0x80;

            return output;
        }

        /// <summary>
        /// Set the receive buffer timeout.
        /// </summary>
        /// <param name="timeout">Packet timeout (in milliseconds).</param>
        private void SetTimeout(int timeout)
        {
            var timeoutPacket = new byte[SetTimeoutPacket.Length];
            SetTimeoutPacket.CopyTo(timeoutPacket, 0);

            var timeoutSamples = 1000 * timeout / TimingResolution; // Timeout as a multiple of the timing resolution

            timeoutPacket[2] = (byte)(timeoutSamples >> 8);
            timeoutPacket[3] = (byte)(timeoutSamples & 0xFF);

            WriteSync(timeoutPacket);
        }

        /// <summary>
        /// Sets the IR receiver input port.
        /// </summary>
        /// <param name="port">The input port.</param>
        private void SetInputPort(InputPort port)
        {
            switch (port)
            {
                case InputPort.Receive:
                    FireStateChanged(new StateChangedEventArgs(RunningState.Started, ReceivingState.Receiving));
                    break;
                case InputPort.Learning:
                    FireStateChanged(new StateChangedEventArgs(RunningState.Started, ReceivingState.Learning));
                    break;
            }

            var inputPortPacket = new byte[SetInputPortPacket.Length];
            SetInputPortPacket.CopyTo(inputPortPacket, 0);

            inputPortPacket[2] = (byte)(port + 1);

            WriteSync(inputPortPacket);
        }

        /// <summary>
        /// Sets the IR blaster port.
        /// </summary>
        /// <param name="port">The output port.</param>
        private void SetOutputPort(int port)
        {
            byte[] portPacket;
            switch (_deviceType)
            {
                case DeviceType.Microsoft:
                    portPacket = MicrosoftPorts[port];
                    break;
                case DeviceType.SmkTopseed:
                    portPacket = SmkTopseedPorts[port];
                    break;
                default:
                    throw new InvalidOperationException("Invalid device type");
            }

            WriteSync(portPacket);
        }

        /// <summary>
        /// Sets the IR output carrier frequency.
        /// </summary>
        /// <param name="carrier">The IR carrier frequency.</param>
        private void SetCarrierFrequency(int carrier)
        {
            if (carrier == IRCode.CarrierFrequencyUnknown)
            {
                carrier = IRCode.CarrierFrequencyDefault;
                DebugWriteLine("SetCarrierFrequency(): No carrier frequency specificied, using default ({0})", carrier);
            }
            else
            {
                DebugWriteLine("SetCarrierFrequency({0})", carrier);
            }

            var carrierPacket = new byte[SetCarrierFreqPacket.Length];
            SetCarrierFreqPacket.CopyTo(carrierPacket, 0);

            if (carrier != IRCode.CarrierFrequencyDCMode)
            {
                for (var scaler = 1; scaler <= 4; scaler++)
                {
                    var divisor = (10000000 >> (2 * scaler)) / carrier;

                    if (divisor <= 0xFF)
                    {
                        carrierPacket[2] = (byte)scaler;
                        carrierPacket[3] = (byte)divisor;
                        break;
                    }
                }
            }

            WriteSync(carrierPacket);
        }

        /// <summary>
        /// Start the device read thread.
        /// </summary>
        private void StartReadThread()
        {
            DebugWriteLine("StartReadThread()");

            if (_readThread != null)
            {
                DebugWriteLine("Read thread already started");
                return;
            }

            _stopReadThread = new ManualResetEvent(false);
            _readThreadMode = ReadThreadMode.Receiving;

            _readThread = new Thread(ReadThread)
                              {
                                  Name = "MicrosoftMceTransceiver.DriverXP.ReadThread",
                                  IsBackground = true
                              };
            _readThread.Start();

            FireStateChanged(new StateChangedEventArgs(RunningState.Started));
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

            try
            {
                _readThreadMode = ReadThreadMode.Stop;
                _stopReadThread.Set();
                if (Thread.CurrentThread != _readThread)
                    _readThread.Join(PacketTimeout * 2);

                //_readThread.Abort();
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());
            }
            finally
            {
                _stopReadThread.Close();
                _stopReadThread = null;

                _readThread = null;
            }
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

            _eHomeHandle = CreateFile(_devicePath, CreateFileAccessTypes.GenericRead | CreateFileAccessTypes.GenericWrite,
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
            if (!success)
                DebugWriteLine("Warning: Failed to initialize device removal notification");

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
            byte[] packetBytes;

            WaitHandle waitHandle = new ManualResetEvent(false);
            SafeHandle safeWaitHandle = waitHandle.SafeWaitHandle;
            var waitHandles = new WaitHandle[] { waitHandle, _stopReadThread };

            var deviceBufferPtr = IntPtr.Zero;

            var success = false;
            safeWaitHandle.DangerousAddRef(ref success);
            if (!success)
                throw new InvalidOperationException("Failed to initialize safe wait handle");

            try
            {
                var dangerousWaitHandle = safeWaitHandle.DangerousGetHandle();

                var overlapped = new DeviceIoOverlapped();
                overlapped.ClearAndSetEvent(dangerousWaitHandle);

                deviceBufferPtr = Marshal.AllocHGlobal(DeviceBufferSize);

                while (_readThreadMode != ReadThreadMode.Stop)
                {
                    int lastError;
                    int bytesRead;

                    var readDevice = ReadFile(_eHomeHandle, deviceBufferPtr, DeviceBufferSize, out bytesRead,
                                               overlapped.Overlapped);
                    lastError = Marshal.GetLastWin32Error();

                    if (!readDevice)
                    {
                        if (lastError != ErrorSuccess && lastError != ErrorIoPending)
                            throw new Win32Exception(lastError);

                        while (true)
                        {
                            int handle = WaitHandle.WaitAny(waitHandles, 2 * PacketTimeout, false);

                            if (handle == ErrorWaitTimeout)
                                continue;

                            if (handle == 0)
                                break;

                            if (handle == 1)
                                throw new ThreadInterruptedException("Read thread stopping by request");

                            throw new InvalidOperationException(String.Format("Invalid wait handle return: {0}", handle));
                        }

                        bool getOverlapped = GetOverlappedResult(_eHomeHandle, overlapped.Overlapped, out bytesRead, true);
                        lastError = Marshal.GetLastWin32Error();

                        if (!getOverlapped && lastError != ErrorSuccess)
                            throw new Win32Exception(lastError);
                    }

                    if (bytesRead == 0)
                        continue;

                    packetBytes = new byte[bytesRead];
                    Marshal.Copy(deviceBufferPtr, packetBytes, 0, bytesRead);

                    DebugWrite("Received bytes ({0}): ", bytesRead);
                    DebugDump(packetBytes);

                    int[] timingData = null;

                    if (_decodeCarry != 0 || packetBytes[0] >= 0x81 && packetBytes[0] <= 0x9E)
                    {
                        timingData = GetTimingDataFromPacket(packetBytes);
                    }
                    else
                    {
                        double firmware = 0.0;

                        int indexOfFF = Array.IndexOf(packetBytes, (byte)0xFF);
                        while (indexOfFF != -1)
                        {
                            if (packetBytes.Length > indexOfFF + 2 && packetBytes[indexOfFF + 1] == 0x0B) // FF 0B XY - Firmware X.Y00
                            {
                                byte b1 = packetBytes[indexOfFF + 2];

                                firmware += (b1 >> 4) + (0.1 * (b1 & 0x0F));
                                DebugWriteLine("Firmware: {0}", firmware);
                            }

                            if (packetBytes.Length > indexOfFF + 2 && packetBytes[indexOfFF + 1] == 0x1B) // FF 1B XY - Firmware 0.0XY
                            {
                                byte b1 = packetBytes[indexOfFF + 2];

                                firmware += (0.01 * (b1 >> 4)) + (0.001 * (b1 & 0x0F));
                                DebugWriteLine("Firmware: {0}", firmware);
                            }

                            if (packetBytes.Length > indexOfFF + 1)
                                indexOfFF = Array.IndexOf(packetBytes, (byte)0xFF, indexOfFF + 1);
                            else
                                break;
                        }
                    }

                    switch (_readThreadMode)
                    {
                        case ReadThreadMode.Receiving:
                            {
                                var code = new IRCode(timingData);
                                code.FinalizeData();
                                FireCodeReceived(new CodeReceivedEventArgs(code));
                                break;
                            }

                        case ReadThreadMode.Learning:
                            {
                                if (timingData == null)
                                {
                                    if (_learningCode.TimingData.Length > 0)
                                    {
                                        _learningCode = null;
                                        _readThreadMode = ReadThreadMode.LearningFailed;
                                    }
                                    break;
                                }

                                if (_learningCode == null)
                                    throw new InvalidOperationException("Learning not initialised correctly, _learningCode object is null");

                                _learningCode.AddTimingData(timingData);

                                // Example: 9F 01 02 9F 15 00 BE 80
                                int indexOf9F = Array.IndexOf(packetBytes, (byte)0x9F);
                                while (indexOf9F != -1)
                                {
                                    if (packetBytes.Length > indexOf9F + 3 && packetBytes[indexOf9F + 1] == 0x15) // 9F 15 XX XX
                                    {
                                        byte b1 = packetBytes[indexOf9F + 2];
                                        byte b2 = packetBytes[indexOf9F + 3];

                                        int onTime, onCount;
                                        GetIrCodeLengths(_learningCode, out onTime, out onCount);

                                        double carrierCount = (b1 * 256) + b2;

                                        if (carrierCount / onCount < 2.0)
                                        {
                                            _learningCode.Carrier = IRCode.CarrierFrequencyDCMode;
                                        }
                                        else
                                        {
                                            double carrier = 1000000 * carrierCount / onTime;

                                            // TODO: Double-Check this calculation.
                                            if (carrier > 32000)
                                            {
                                                _learningCode.Carrier = (int)(carrier + 0.05 * carrier - 0.666667);
                                                // was: _learningCode.Carrier = (int) (carrier + 0.05*carrier - 32000/48000);
                                            }
                                            else
                                            {
                                                _learningCode.Carrier = (int)carrier;
                                            }
                                        }

                                        _readThreadMode = ReadThreadMode.LearningDone;
                                        break;
                                    }

                                    if (packetBytes.Length > indexOf9F + 1)
                                    {
                                        indexOf9F = Array.IndexOf(packetBytes, (byte)0x9F, indexOf9F + 1);
                                    }
                                    else
                                    {
                                        _readThreadMode = ReadThreadMode.LearningFailed;
                                        break;
                                    }
                                }

                                break;
                            }
                    }
                }
                FireStateChanged(new StateChangedEventArgs(RunningState.Stopping));
            }
            catch (ThreadInterruptedException ex)
            {
                DebugWriteLine(ex.Message);

                if (_eHomeHandle != null)
                    CancelIo(_eHomeHandle);
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());

                if (_eHomeHandle != null)
                    CancelIo(_eHomeHandle);
            }
            finally
            {
                if (deviceBufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(deviceBufferPtr);

                safeWaitHandle.DangerousRelease();
                waitHandle.Close();
            }

            DebugWriteLine("Read Thread Ended");
            FireStateChanged(new StateChangedEventArgs(RunningState.Stopped));
        }

        /// <summary>
        /// Synchronously write a packet of bytes to the device.
        /// </summary>
        /// <param name="data">Packet to write to device.</param>
        private void WriteSync(byte[] data)
        {
            DebugWrite("WriteSync({0}): ", data.Length);
            DebugDump(data);

            if (!_deviceAvailable)
                throw new InvalidOperationException("Device not available");

            WaitHandle waitHandle = new ManualResetEvent(false);
            SafeHandle safeWaitHandle = waitHandle.SafeWaitHandle;
            var waitHandles = new WaitHandle[] { waitHandle };

            try
            {
                var success = false;
                safeWaitHandle.DangerousAddRef(ref success);
                if (!success)
                    throw new InvalidOperationException("Failed to initialize safe wait handle");

                var overlapped = new DeviceIoOverlapped();
                overlapped.ClearAndSetEvent(safeWaitHandle.DangerousGetHandle());

                int bytesWritten;
                var writeDevice = WriteFile(_eHomeHandle, data, data.Length, out bytesWritten, overlapped.Overlapped);
                var lastError = Marshal.GetLastWin32Error();

                if (writeDevice)
                    return;

                if (lastError != ErrorSuccess && lastError != ErrorIoPending)
                    throw new Win32Exception(lastError);

                var handle = WaitHandle.WaitAny(waitHandles, WriteSyncTimeout, false);

                if (handle == ErrorWaitTimeout)
                    throw new TimeoutException("Timeout trying to write data to device");

                if (handle != 0)
                    throw new InvalidOperationException(String.Format("Invalid wait handle return: {0}", handle));

                var getOverlapped = GetOverlappedResult(_eHomeHandle, overlapped.Overlapped, out bytesWritten, true);
                lastError = Marshal.GetLastWin32Error();

                if (!getOverlapped && lastError != ErrorSuccess)
                    throw new Win32Exception(lastError);

                Thread.Sleep(PacketTimeout);
            }
            catch
            {
                if (_eHomeHandle != null)
                    CancelIo(_eHomeHandle);

                throw;
            }
            finally
            {
                safeWaitHandle.DangerousRelease();
                waitHandle.Close();
            }
        }

        /// <summary>
        /// Turn raw packet data int pulse timing data.
        /// </summary>
        /// <param name="packet">The raw device packet.</param>
        /// <returns>Timing data.</returns>
        private int[] GetTimingDataFromPacket(byte[] packet)
        {
            // TODO: Remove this try/catch block once the IndexOutOfRangeException is corrected...
            try
            {
                if (_decodeCarry != 0)
                {
                    DebugWriteLine("Decode Carry EXISTS: {0}", _decodeCarry);
                    DebugDump(packet);
                }

                var timingData = new List<int>();

                var len = 0;

                for (var index = 0; index < packet.Length; )
                {
                    var curByte = packet[index];

                    if (_decodeCarry == 0)
                    {
                        if (curByte == 0x9F)
                            break;

                        if (curByte < 0x81 || curByte > 0x9E)
                            return null;
                    }

                    var bytes = _decodeCarry;
                    if (_decodeCarry == 0)
                        bytes = curByte & 0x7F;
                    else
                        _decodeCarry = 0;

                    if (index + bytes + 1 > packet.Length)
                    {
                        _decodeCarry = (index + bytes + 1) - packet.Length;
                        bytes -= _decodeCarry;

                        DebugWriteLine("Decode Carry SET: {0}", _decodeCarry);
                        DebugDump(packet);
                    }

                    int j;
                    for (j = index + 1; j < index + bytes + 1; j++)
                    {
                        curByte = packet[j];

                        if ((curByte & 0x80) != 0)
                            len += (curByte & 0x7F);
                        else
                            len -= curByte;

                        if ((curByte & 0x7F) != 0x7F)
                        {
                            timingData.Add(len * TimingResolution);
                            len = 0;
                        }
                    }

                    index = j;
                }

                if (len != 0)
                    timingData.Add(len * TimingResolution);

                DebugWrite("Received timing:    ");
                DebugDump(timingData.ToArray());

                return timingData.ToArray();
            }
            catch (Exception ex)
            {
                DebugWriteLine(ex.ToString());
                DebugWrite("Method Input:       ");
                DebugDump(packet);

                return null;
            }
        }

        /// <summary>
        /// Determines the pulse count and total time of the supplied IrCode.
        /// This is used to determine the carrier frequency.
        /// </summary>
        /// <param name="code">The IrCode to analyse.</param>
        /// <param name="pulseTime">The total ammount of pulse time.</param>
        /// <param name="pulseCount">The total count of pulses.</param>
        private static void GetIrCodeLengths(IRCode code, out int pulseTime, out int pulseCount)
        {
            pulseTime = 0;
            pulseCount = 0;

            foreach (int time in code.TimingData)
            {
                if (time > 0)
                {
                    pulseTime += time;
                    pulseCount++;
                }
            }
        }

        #endregion Implementation
    }
}
