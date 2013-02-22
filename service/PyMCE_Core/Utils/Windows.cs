using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PyMCE_Core.Utils
{
    class Windows
    {
        #region Interop

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process(
             [In] IntPtr hProcess,
             [Out] out bool lpSystemInfo);

        #endregion

        public static bool IsSystem64Bit()
        {
            //IsWow64Process is not supported under Windows2000 ( ver 5.0 )
            int osver = Environment.OSVersion.Version.Major * 10 + Environment.OSVersion.Version.Minor;
            if (osver <= 50) return false;

            Process p = Process.GetCurrentProcess();
            IntPtr handle = p.Handle;
            bool isWow64;
            bool success = IsWow64Process(handle, out isWow64);
            if (!success)
            {
                throw new Win32Exception();
            }
            return isWow64;
        }
    }
}
