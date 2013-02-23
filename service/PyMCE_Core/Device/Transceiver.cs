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
using System.Linq;
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

    #region Delegates

    public delegate LearnResult LearnDelegate();
    public delegate void LearnCompletedDelegate(LearnResult result);

    #endregion

    #region Shell Classes

    public class LearnResult
    {
        public LearnStatus Status { get; private set; }
        public byte[] Data { get; private set; }

        internal LearnResult(LearnStatus status, byte[] data)
        {
            Status = status;
            Data = data;
        }
    }

    internal class LearnAsyncState
    {
        public LearnDelegate Delegate { get; private set; }
        public LearnCompletedDelegate Callback { get; private set; }

        public LearnAsyncState(LearnDelegate del, LearnCompletedDelegate callback)
        {
            Delegate = del;
            Callback = callback;
        }
    }

    #endregion

    public class Transceiver
    {
        #region Constants

        private const string AutomaticButtonsRegKey = @"SYSTEM\CurrentControlSet\Services\HidIr\Remotes\745a17a0-74d3-11d0-b6fe-00a0c90f57da";

        private const int VistaVersionNumber = 6;

        private static readonly Guid MicrosoftGuid = new Guid(0x7951772d, 0xcd50, 0x49b7, 0xb1, 0x03, 0x2b, 0xaa, 0xc4, 0x94, 0xfc, 0x57);
        private static readonly Guid ReplacementGuid = new Guid(0x00873fdf, 0x61a8, 0x11d1, 0xaa, 0x5e, 0x00, 0xc0, 0x4f, 0xb1, 0x72, 0x8b);

        #endregion Constants

        #region Variables

        #region Configuration

        private bool _disableMceServices = true;
        private int _learnTimeout = 10000;

        #endregion

        private Driver _driver;

        private object _learnLock = new object();

        #endregion

        #region Events

        public event CodeReceivedDelegate CodeReceived;

        public event StateChangedDelegate StateChanged;

        #endregion

        #region Public Methods

        #region Learn

        public LearnStatus Learn(out byte[] data)
        {
            IRCode code;
            LearnStatus status;

            lock (_learnLock)
            {
                status = _driver.Learn(_learnTimeout, out code);
            }

            data = code != null ? code.ToByteArray() : null;
            return status;
        }

        public LearnResult Learn()
        {
            byte[] data;
            var status = Learn(out data);

            return new LearnResult(status, data);
        }

        public void LearnAsync(LearnCompletedDelegate callback)
        {
            LearnDelegate learnDelegate = Learn;

            learnDelegate.BeginInvoke(LearnAsyncCallback, new LearnAsyncState(learnDelegate, callback));
        }

        internal void LearnAsyncCallback(IAsyncResult result)
        {
            if (!(result.AsyncState is LearnAsyncState)) return;

            var state = (LearnAsyncState)result.AsyncState;
            state.Callback(state.Delegate.EndInvoke(result));
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
            Trace.WriteLine("Start MicrosoftMceTransceiver");

            if (_driver != null)
                throw new InvalidOperationException("MicrosoftMceTransceiver already started");

            if (_disableMceServices)
                DisableMceServices();

            Guid deviceGuid;
            string devicePath;

            Driver newDriver;

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

            _driver = newDriver;
            _driver.StateChangedCallback = Driver_StateChangedCallback;
            _driver.CodeReceivedCallback = Driver_CodeReceivedCallback;
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

        #endregion

        #region Properties

        public RunningState CurrentRunningState
        {
            get
            {
                return _driver != null ? _driver.CurrentRunningState : RunningState.Stopped;
            }
        }

        public ReceivingState CurrentReceivingState
        {
            get
            {
                return _driver != null ? _driver.CurrentReceivingState : ReceivingState.None;
            }
        }

        #endregion

        #region Driver Callbacks

        private void Driver_CodeReceivedCallback(object sender, CodeReceivedEventArgs codeReceivedEventArgs)
        {
            if (CodeReceived != null)
                CodeReceived(sender, codeReceivedEventArgs);
        }

        private void Driver_StateChangedCallback(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            if (StateChanged != null)
                StateChanged(sender, stateChangedEventArgs);
        }

        #endregion

        #region Internal Methods

        internal static bool CheckAutomaticButtons()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(AutomaticButtonsRegKey, false))
            {
                return (key.GetValue("CodeSetNum0", null) != null);
            }
        }

        internal static void EnableAutomaticButtons()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(AutomaticButtonsRegKey, true))
            {
                key.SetValue("CodeSetNum0", 1, RegistryValueKind.DWord);
                key.SetValue("CodeSetNum1", 2, RegistryValueKind.DWord);
                key.SetValue("CodeSetNum2", 3, RegistryValueKind.DWord);
                key.SetValue("CodeSetNum3", 4, RegistryValueKind.DWord);
            }
        }

        internal static void DisableAutomaticButtons()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(AutomaticButtonsRegKey, true))
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
                var services = ServiceController.GetServices();
                foreach (var service in services)
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
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }

            // XP & Vista ...
            // Kill Microsoft MCE ehtray process (if it exists)
            try
            {
                var processes = Process.GetProcesses();
                foreach (var proc in processes.Where(proc => proc.ProcessName.Equals("ehtray", StringComparison.OrdinalIgnoreCase)))
                    proc.Kill();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
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

        #endregion
    }
}
