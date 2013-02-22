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
using System.Diagnostics;
using System.ServiceProcess;
using PyMCE.Core.Infrared;
using Microsoft.Win32;

namespace PyMCE.Core.Device
{
    #region Enumerations

    /// <summary>
    /// The blaster port to send IR Commands to.
    /// </summary>
    public enum BlasterPort
    {
        /// <summary>
        /// Send IR Commands to both blaster ports.
        /// </summary>
        Both = 0,
        /// <summary>
        /// Send IR Commands to blaster port 1 only.
        /// </summary>
        Port_1 = 1,
        /// <summary>
        /// Send IR Commands to blaster port 2 only.
        /// </summary>
        Port_2 = 2
    }

    /// <summary>
    /// Provides information about the status of learning an infrared command.
    /// </summary>
    public enum LearnStatus
    {
        /// <summary>
        /// Failed to learn infrared command.
        /// </summary>
        Failure,
        /// <summary>
        /// Succeeded in learning infrared command.
        /// </summary>
        Success,
        /// <summary>
        /// Infrared command learning timed out.
        /// </summary>
        Timeout,
    }

    #endregion

    public class Transceiver
    {
        #region Constants

        private const string AutomaticButtonsRegKey =
            @"SYSTEM\CurrentControlSet\Services\HidIr\Remotes\745a17a0-74d3-11d0-b6fe-00a0c90f57da";

        private const int VistaVersionNumber = 6;

        private static readonly Guid MicrosoftGuid = new Guid(0x7951772d, 0xcd50, 0x49b7, 0xb1, 0x03, 0x2b, 0xaa, 0xc4, 0x94, 0xfc, 0x57);

        private static readonly Guid ReplacementGuid = new Guid(0x00873fdf, 0x61a8, 0x11d1, 0xaa, 0x5e, 0x00, 0xc0, 0x4f, 0xb1, 0x72, 0x8b);

        #endregion Constants

        #region Variables

        #region Configuration

        private bool _disableAutomaticButtons;
        private bool _disableMceServices = true;

        private int _learnTimeout = 10000;

        #endregion

        private Driver _driver;
        private bool _ignoreAutomaticButtons;

        #endregion

        #region Learn

        public LearnStatus Learn(out byte[] data)
        {
            IRCode code;

            var status = _driver.Learn(_learnTimeout, out code);

            data = code != null ? code.ToByteArray() : null;
            return status;
        }

        #endregion

        #region Transmit

        public string[] AvailablePorts
        {
            get { return Enum.GetNames(typeof (BlasterPort)); }
        }

        public bool Transmit(string port, byte[] data)
        {
            var blasterPort = BlasterPort.Both;
            try
            {
                blasterPort = (BlasterPort) Enum.Parse(typeof (BlasterPort), port, true);
            }
            catch (Exception)
            {
                Debug.WriteLine(string.Format("Invalid Blaster Port ({0}), using default {1}", port, blasterPort));
            }

            var code = IRCode.FromByteArray(data);

            if(code == null)
                throw new ArgumentException("Invalid IR Command data", "data");

            _driver.Send(code, (int)blasterPort);

            return true;
        }

        #endregion

        #region Control Methods

        public void Start()
        {
#if TRACE
            Trace.WriteLine("Start MicrosoftMceTransceiver");
#endif
            if (_driver != null)
                throw new InvalidOperationException("MicrosoftMceTransceiver already started");

            //LoadSettings();

            // Put this in a try...catch so that if the registry keys don't exist we don't throw an ugly exception.
            try
            {
                _ignoreAutomaticButtons = CheckAutomaticButtons();
            }
            catch
            {
                _ignoreAutomaticButtons = false;
            }

            if (_disableMceServices)
                DisableMceServices();

            Guid deviceGuid;
            string devicePath;

            Driver newDriver = null;

            if (FindDevice(out deviceGuid, out devicePath))
            {
                if (deviceGuid == MicrosoftGuid)
                {
                    if (Environment.OSVersion.Version.Major >= VistaVersionNumber)
                    {
                        newDriver = new DriverVista(deviceGuid, devicePath);
                    }
                    else
                    {
                        newDriver = new DriverXP(deviceGuid, devicePath);
                    }
                }
                else
                {
                    newDriver = new DriverReplacement(deviceGuid, devicePath);
                }
            }
            else
            {
                throw new InvalidOperationException("Device not found");
            }

            newDriver.Start();

            _driver = newDriver;
        }

        public void Suspend()
        {
            if(_driver != null)
                _driver.Suspend();
        }

        public void Resume()
        {
            if(_driver != null)
                _driver.Resume();
        }

        public void Stop()
        {
            if (_driver == null) return;

            try
            {
                _driver.Stop();
            }
            finally
            {
                _driver = null;
            }
        }

        #endregion

        internal static bool CheckAutomaticButtons()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(AutomaticButtonsRegKey, false))
            {
                return (key.GetValue("CodeSetNum0", null) != null);
            }
        }

        internal static void EnableAutomaticButtons()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(AutomaticButtonsRegKey, true))
            {
                key.SetValue("CodeSetNum0", 1, RegistryValueKind.DWord);
                key.SetValue("CodeSetNum1", 2, RegistryValueKind.DWord);
                key.SetValue("CodeSetNum2", 3, RegistryValueKind.DWord);
                key.SetValue("CodeSetNum3", 4, RegistryValueKind.DWord);
            }
        }

        internal static void DisableAutomaticButtons()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(AutomaticButtonsRegKey, true))
            {
                key.DeleteValue("CodeSetNum0", false);
                key.DeleteValue("CodeSetNum1", false);
                key.DeleteValue("CodeSetNum2", false);
                key.DeleteValue("CodeSetNum3", false);
            }
        }

        private static void DisableMceServices()
        {
            // "HKLM\SYSTEM\CurrentControlSet\Services\<service name>\Start"
            // 2 for automatic, 3 manual , 4 disabled


            // Vista ...
            // Stop Microsoft MCE ehRecvr, mcrdsvc and ehSched processes (if they exist)
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                foreach (ServiceController service in services)
                {
                    if (service.ServiceName.Equals("ehRecvr", StringComparison.OrdinalIgnoreCase))
                    {
                        if (service.Status != ServiceControllerStatus.Stopped &&
                            service.Status != ServiceControllerStatus.StopPending)
                            service.Stop();
                    }
                    else if (service.ServiceName.Equals("ehSched", StringComparison.OrdinalIgnoreCase))
                    {
                        if (service.Status != ServiceControllerStatus.Stopped &&
                            service.Status != ServiceControllerStatus.StopPending)
                            service.Stop();
                    }
                    else if (service.ServiceName.Equals("mcrdsvc", StringComparison.OrdinalIgnoreCase))
                    {
                        if (service.Status != ServiceControllerStatus.Stopped &&
                            service.Status != ServiceControllerStatus.StopPending)
                            service.Stop();
                    }
                }
            }
#if TRACE
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
#else
      catch
      {
      }
#endif

            // XP & Vista ...
            // Kill Microsoft MCE ehtray process (if it exists)
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process proc in processes)
                    if (proc.ProcessName.Equals("ehtray", StringComparison.OrdinalIgnoreCase))
                        proc.Kill();
            }
#if TRACE
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
#else
      catch
      {
      }
#endif
        }

        private static bool FindDevice(out Guid deviceGuid, out string devicePath)
        {
            devicePath = null;

            // Try eHome driver
            deviceGuid = MicrosoftGuid;
            try
            {
                devicePath = Driver.Find(deviceGuid);

                if (!String.IsNullOrEmpty(devicePath))
                    return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            // Try Replacement driver
            deviceGuid = ReplacementGuid;
            try
            {
                devicePath = Driver.Find(deviceGuid);

                if (!String.IsNullOrEmpty(devicePath))
                    return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            return false;
        }
    }
}
