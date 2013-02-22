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
using PyMCE_Core.Infrared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PyMCE_Core.Device
{
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

        #endregion

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

        #endregion

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

        #endregion

        #region Variables

        protected Guid DeviceGuid;
        protected string DevicePath;

        #endregion

        #region Constructor

        protected Driver() { }

        protected Driver(Guid deviceGuid, string devicePath)
        {
            if(String.IsNullOrEmpty(devicePath))
                throw new ArgumentNullException("devicePath");

            DeviceGuid = deviceGuid;
            DevicePath = devicePath;
        }

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
        public abstract Transceiver.LearnStatus Learn(int learnTimeout, out IRCode learned);

        /// <summary>
        /// Send an IR Command.
        /// </summary>
        /// <param name="code">IR Command data to send.</param>
        /// <param name="port">IR port to send to.</param>
        public abstract void Send(IRCode code, int port);

        #endregion

        #region Static Methods

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
                    !SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, IntPtr.Zero, 0, ref cbData,
                                                     IntPtr.Zero) && cbData == 0)
                {
                    SetupDiDestroyDeviceInfoList(handle);
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var deviceInterfaceDetailData = new DeviceInterfaceDetailData();
                deviceInterfaceDetailData.Size = IntPtr.Size == 8 ? 8 : 5;

                if (
                    !SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData, ref deviceInterfaceDetailData,
                                                     cbData, IntPtr.Zero, IntPtr.Zero))
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

        /// <summary>
        /// Dumps an Array to the debug output file.
        /// </summary>
        /// <param name="array">The array.</param>
        protected static void DebugDump(Array array)
        {
            foreach (var item in array)
            {
                if (item is byte) Debug.Write(string.Format("{0:X2}", (byte)item));
                else if (item is ushort) Debug.Write(string.Format("{0:X4}", (ushort)item));
                else if (item is int) Debug.Write(string.Format("{1}{0}", (int)item, (int)item > 0 ? "+" : String.Empty));
                else Debug.Write(string.Format("{0}", item));

                Debug.Write(", ");
            }

            Debug.WriteLine("");
        }

        #endregion
    }
}
