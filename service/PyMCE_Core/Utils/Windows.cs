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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PyMCE.Core.Utils
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
            var osver = Environment.OSVersion.Version.Major * 10 + Environment.OSVersion.Version.Minor;
            if (osver <= 50) return false;

            var p = Process.GetCurrentProcess();
            var handle = p.Handle;
            bool isWow64;
            var success = IsWow64Process(handle, out isWow64);
            if (!success)
            {
                throw new Win32Exception();
            }
            return isWow64;
        }
    }
}
