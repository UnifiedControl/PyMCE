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
using PyMCE_Core.Infrared;

namespace PyMCE_Core.Device
{
    public class Transceiver
    {
        #region Constants

        private const int VistaVersionNumber = 6;

        private static readonly Guid MicrosoftGuid = new Guid(0x7951772d, 0xcd50, 0x49b7, 0xb1, 0x03, 0x2b, 0xaa, 0xc4, 0x94, 0xfc, 0x57);

        private static readonly Guid ReplacementGuid = new Guid(0x00873fdf, 0x61a8, 0x11d1, 0xaa, 0x5e, 0x00, 0xc0, 0x4f, 0xb1, 0x72, 0x8b);

        #endregion Constants

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

        #region Variables

        #region Configuration

        // TODO: Move to Configuration class

        private int _learnTimeout = 10000;

        #endregion

        private Driver _driver;

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
            Debug.WriteLine("Start Device.Transceiver");

            Guid deviceGuid;
            string devicePath;

            Driver driver = null;

            if (FindDevice(out deviceGuid, out devicePath))
            {
                if (deviceGuid == MicrosoftGuid)
                {
                    if (Environment.OSVersion.Version.Major >= VistaVersionNumber)
                    {
                        driver = new DriverVista(deviceGuid, devicePath);
                    }
                    else
                    {
                        driver = new DriverXP(deviceGuid, devicePath);
                    }
                }
                else
                {
                    driver = new DriverReplacement(deviceGuid, devicePath);
                }
            }
            else
            {
                throw new InvalidOperationException("Device not found");
            }

            _driver = driver;

            _driver.Start();
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
