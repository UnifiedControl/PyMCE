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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PyMCE.Core.Device
{
    #region Shell Classes

    public class CodeReceivedEventArgs : EventArgs
    {
        public IRCode Code { get; private set; }

        public CodeReceivedEventArgs(IRCode code)
        {
            Code = code;
        }
    }

    public enum RunningState
    {
        Starting,
        Started,

        Stopping,
        Stopped
    }

    public enum ReceivingState
    {
        None,
        Receiving,
        Learning
    }

    public class StateChangedEventArgs : EventArgs
    {
        public RunningState RunningState { get; private set; }
        public ReceivingState ReceivingState { get; private set; }

        public StateChangedEventArgs(RunningState runningState)
        {
            RunningState = runningState;
            ReceivingState = ReceivingState.None;
        }

        public StateChangedEventArgs(RunningState runningState, ReceivingState receivingState)
        {
            RunningState = runningState;
            ReceivingState = receivingState;
        }
    }

    #endregion

    #region Delegates

    public delegate void CodeReceivedDelegate(object sender, CodeReceivedEventArgs e);
    public delegate void StateChangedDelegate(object sender, StateChangedEventArgs e);

    #endregion

    /// <summary>
    /// Base class for the different MCE device driver access classes.
    /// </summary>
    internal abstract class Driver
    {
        #region Constants

        /// <Summary>
        /// Overlapped I/O Operation Is In Progress.
        /// </Summary>
        public const int ErrorIoPending = 997;

        /// <Summary>
        /// The Specified Module Could Not Be Found.
        /// </Summary>
        public const int ErrorModNotFound = 126;

        /// <Summary>
        /// No More Data Is Available.
        /// </Summary>
        public const int ErrorNoMoreItems = 259;

        /// <Summary>
        /// The Operation Completed Successfully.
        /// </Summary>
        public const int ErrorSuccess = 0;

        /// <Summary>
        /// The Wait Operation Timed Out.
        /// </Summary>
        public const int ErrorWaitTimeout = 258;

        #endregion Constants

        #region Enumerations

        #region Nested type: CreateFileAccessTypes

        [Flags]
        protected enum CreateFileAccessTypes : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        #endregion

        #region Nested type: CreateFileAttributes

        [Flags]
        protected enum CreateFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000,
        }

        #endregion

        #region Nested type: CreateFileDisposition

        protected enum CreateFileDisposition : uint
        {
            None = 0,
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5,
        }

        #endregion

        #region Nested type: CreateFileShares

        [Flags]
        protected enum CreateFileShares : uint
        {
            None = 0x00,
            Read = 0x01,
            Write = 0x02,
            Delete = 0x04,
        }

        #endregion

        #region Nested type: Digcfs

        [Flags]
        private enum Digcfs : uint
        {
            None = 0x00,
            Default = 0x01,
            Present = 0x02,
            AllClasses = 0x04,
            Profile = 0x08,
            DeviceInterface = 0x10,
        }

        #endregion

        #endregion Enumerations

        #region Structures

        #region Nested type: DeviceInfoData

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DeviceInfoData
        {
            public int Size;
            public Guid Class;
            public int DevInst;
            public IntPtr Reserved;
        }

        #endregion

        #region Nested type: DeviceInterfaceData

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DeviceInterfaceData
        {
            public int Size;
            public Guid Class;
            public int Flags;
            public IntPtr Reserved;
        }

        #endregion

        #region Nested type: DeviceInterfaceDetailData

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct DeviceInterfaceDetailData
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        #endregion

        #endregion Structures

        #region Interop

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool GetOverlappedResult(
          SafeFileHandle handle,
          IntPtr overlapped,
          out int bytesTransferred,
          [MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern SafeFileHandle CreateFile(
          [MarshalAs(UnmanagedType.LPTStr)] string fileName,
          [MarshalAs(UnmanagedType.U4)] CreateFileAccessTypes fileAccess,
          [MarshalAs(UnmanagedType.U4)] CreateFileShares fileShare,
          IntPtr securityAttributes,
          [MarshalAs(UnmanagedType.U4)] CreateFileDisposition creationDisposition,
          [MarshalAs(UnmanagedType.U4)] CreateFileAttributes flags,
          IntPtr templateFile);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool CancelIo(
          SafeFileHandle handle);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
          ref Guid classGuid,
          [MarshalAs(UnmanagedType.LPTStr)] string enumerator,
          IntPtr hwndParent,
          [MarshalAs(UnmanagedType.U4)] Digcfs flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInfo(
          IntPtr handle,
          int index,
          ref DeviceInfoData deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiEnumDeviceInterfaces(
          IntPtr handle,
          ref DeviceInfoData deviceInfoData,
          ref Guid guidClass,
          int memberIndex,
          ref DeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
          IntPtr handle,
          ref DeviceInterfaceData deviceInterfaceData,
          IntPtr unused1,
          int unused2,
          ref uint requiredSize,
          IntPtr unused3);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
          IntPtr handle,
          ref DeviceInterfaceData deviceInterfaceData,
          ref DeviceInterfaceDetailData deviceInterfaceDetailData,
          uint detailSize,
          IntPtr unused1,
          IntPtr unused2);

        [DllImport("setupapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetupDiDestroyDeviceInfoList(
          IntPtr handle);

        #endregion Interop

        #region Variables

        protected Guid _deviceGuid;
        protected string _devicePath;

        #endregion Variables

        #region Constructors

        protected Driver()
        {
        }

        protected Driver(Guid deviceGuid, string devicePath)
        {
            if (String.IsNullOrEmpty(devicePath))
                throw new ArgumentNullException("devicePath");

            _deviceGuid = deviceGuid;
            _devicePath = devicePath;
        }

        #endregion Constructors

        #region Properties

        #region Callbacks

        internal CodeReceivedDelegate CodeReceivedCallback { get; set; }
        protected void FireCodeReceived(CodeReceivedEventArgs e)
        {
            if (CodeReceivedCallback != null)
                CodeReceivedCallback(this, e);
        }

        internal StateChangedDelegate StateChangedCallback { get; set; }
        protected void FireStateChanged(StateChangedEventArgs e)
        {
            if (StateChangedCallback != null &&
                (e.RunningState != CurrentRunningState ||
                 e.ReceivingState != CurrentReceivingState))
            {
                StateChangedCallback(this, e);
                CurrentRunningState = e.RunningState;
                CurrentReceivingState = e.ReceivingState;
            }
        }

        #endregion

        public RunningState CurrentRunningState { get; protected set; }
        public ReceivingState CurrentReceivingState { get; protected set; }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Start using the device.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stop access to the device.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Computer is entering standby, suspend device.
        /// </summary>
        public abstract void Suspend();

        /// <summary>
        /// Computer is returning from standby, resume device.
        /// </summary>
        public abstract void Resume();

        /// <summary>
        /// Learn an IR Command.
        /// </summary>
        /// <param name="learnTimeout">How long to wait before aborting learn.</param>
        /// <param name="learned">Newly learned IR Command.</param>
        /// <returns>Learn status.</returns>
        public abstract LearnStatus Learn(int learnTimeout, out IRCode learned);

        /// <summary>
        /// Send an IR Command.
        /// </summary>
        /// <param name="code">IR Command data to send.</param>
        /// <param name="port">IR port to send to.</param>
        public abstract void Send(IRCode code, int port);

        #endregion Abstract Methods

        #region Static Methods

        /// <summary>
        /// Find the device path for the supplied Device Class Guid.
        /// </summary>
        /// <param name="classGuid">GUID to locate device with.</param>
        /// <returns>Device path.</returns>
        public static string Find(Guid classGuid)
        {
            var handle = SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, Digcfs.DeviceInterface | Digcfs.Present);

            if (handle.ToInt32() == -1)
                return null;

            for (var deviceIndex = 0; ; deviceIndex++)
            {
                var deviceInfoData = new DeviceInfoData();
                deviceInfoData.Size = Marshal.SizeOf(deviceInfoData);

                if (!SetupDiEnumDeviceInfo(handle, deviceIndex, ref deviceInfoData))
                {
                    var lastError = Marshal.GetLastWin32Error();

                    // out of devices or do we have an error?
                    if (lastError != ErrorNoMoreItems && lastError != ErrorModNotFound)
                    {
                        SetupDiDestroyDeviceInfoList(handle);
                        throw new Win32Exception(lastError);
                    }

                    SetupDiDestroyDeviceInfoList(handle);
                    break;
                }

                var deviceInterfaceData = new DeviceInterfaceData();
                deviceInterfaceData.Size = Marshal.SizeOf(deviceInterfaceData);

                if (!SetupDiEnumDeviceInterfaces(handle, ref deviceInfoData, ref classGuid, 0, ref deviceInterfaceData))
                {
                    SetupDiDestroyDeviceInfoList(handle);
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                uint cbData = 0;

                if (
                  !SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, IntPtr.Zero, 0, ref cbData, IntPtr.Zero) &&
                  cbData == 0)
                {
                    SetupDiDestroyDeviceInfoList(handle);
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var deviceInterfaceDetailData = new DeviceInterfaceDetailData
                                                    {
                                                        Size = IntPtr.Size == 8 ? 8 : 5
                                                    };


                if (
                  !SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData, cbData,
                                                   IntPtr.Zero, IntPtr.Zero))
                {
                    SetupDiDestroyDeviceInfoList(handle);
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!String.IsNullOrEmpty(deviceInterfaceDetailData.DevicePath))
                {
                    SetupDiDestroyDeviceInfoList(handle);
                    return deviceInterfaceDetailData.DevicePath;
                }
            }

            return null;
        }

        #endregion Static Methods

        #region Debug

        /// <summary>
        /// Opens a debug output file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        protected static void DebugOpen(string fileName)
        {
            /*try
            {
#if TEST_APPLICATION
        string path = fileName;
#else
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                           String.Format("IR Server Suite\\Logs\\{0}", fileName));
#endif
                _debugFile = new StreamWriter(path, false);
                _debugFile.AutoFlush = true;
            }
#if TRACE
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
#else
      catch
      {
#endif
                _debugFile = null;
            }*/
        }

        /// <summary>
        /// Closes the debug output file.
        /// </summary>
        protected static void DebugClose()
        {
            /*if (_debugFile != null)
            {
                _debugFile.Close();
                _debugFile.Dispose();
                _debugFile = null;
            }*/
        }

        /// <summary>
        /// Writes a line to the debug output file.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="args">Formatting arguments.</param>
        protected static void DebugWriteLine(string line, params object[] args)
        {
            Debug.WriteLine(String.Format(line, args));
        }

        /// <summary>
        /// Writes a string to the debug output file.
        /// </summary>
        /// <param name="text">The string to write.</param>
        /// <param name="args">Formatting arguments.</param>
        protected static void DebugWrite(string text, params object[] args)
        {
            Debug.Write(String.Format(text, args));
        }

        /// <summary>
        /// Writes a new line to the debug output file.
        /// </summary>
        protected static void DebugWriteNewLine()
        {
            Debug.WriteLine(String.Empty);
        }

        /// <summary>
        /// Dumps an Array to the debug output file.
        /// </summary>
        /// <param name="array">The array.</param>
        protected static void DebugDump(Array array)
        {
            foreach (object item in array)
            {
                if (item is byte) DebugWrite("{0:X2}", (byte)item);
                else if (item is ushort) DebugWrite("{0:X4}", (ushort)item);
                else if (item is int) DebugWrite("{1}{0}", (int)item, (int)item > 0 ? "+" : String.Empty);
                else DebugWrite("{0}", item);

                DebugWrite(", ");
            }

            DebugWriteNewLine();
        }

        #endregion Debug
    }
}
