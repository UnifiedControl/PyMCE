
using System.Linq;
using PyMCE.Core.Infrared;
using PyMCE.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using System.IO;
namespace PyMCE.Core.Device.Agent
{
    public abstract class AgentBase
    {
        #region Constants

        internal const string AutomaticButtonsRegKey = @"SYSTEM\CurrentControlSet\Services\HidIr\Remotes\745a17a0-74d3-11d0-b6fe-00a0c90f57da";

        internal const int VistaVersionNumber = 6;

        internal static readonly Guid MicrosoftGuid = new Guid(0x7951772d, 0xcd50, 0x49b7, 0xb1, 0x03, 0x2b, 0xaa, 0xc4, 0x94, 0xfc, 0x57);
        internal static readonly Guid ReplacementGuid = new Guid(0x00873fdf, 0x61a8, 0x11d1, 0xaa, 0x5e, 0x00, 0xc0, 0x4f, 0xb1, 0x72, 0x8b);

        internal static readonly Dictionary<string, InterferenceLevel> Interference
            = new Dictionary<string, InterferenceLevel>
                  {
                      {
                          "AlternateMceIrService",
                          InterferenceLevel.Service | InterferenceLevel.Learn
                      }
                  };

        #endregion

        #region Variables

        private int _learnTimeout = 10000;

        #endregion

        #region Properties

        public int LearnTimeout
        {
            get { return _learnTimeout; }
            set { _learnTimeout = value; }
        }

        internal Driver Driver { get; set; }
        internal StateChangedDelegate StateChangedCallback { get; set; }
        internal CodeReceivedDelegate CodeReceivedCallback { get; set; }

        public virtual string PipeName
        {
            get { return null; }
            set { throw new NotImplementedException(); }
        }

        public virtual RunningState CurrentRunningState
        {
            get { return Driver != null ? Driver.CurrentRunningState : RunningState.Unknown; }
        }

        public virtual ReceivingState CurrentReceivingState
        {
            get { return Driver != null ? Driver.CurrentReceivingState : ReceivingState.Unknown; }
        }

        #endregion

        #region Abstract Methods

        public abstract LearnStatus Learn(out IRCode code);

        public abstract bool Transmit(string port, IRCode code);

        #region Control

        public abstract void Start(InterferenceLevel[] ignore = null,
            bool disableMceServices = true);

        public abstract void Suspend();

        public abstract void Resume();

        public abstract void Stop();

        #endregion

        #endregion

        #region Internal Methods

        /// <summary>
        /// Checks that there are no programs or services running that are known
        /// to interfere with the transceiver input/output.
        /// </summary>
        /// <returns>Dictionary detailing programs/services that could interfere and their "Interference Level"</returns>
        internal static Dictionary<string, InterferenceLevel> InterferenceCheck()
        {
            Log.Trace("InterferenceCheck()");
            var found = new Dictionary<string, InterferenceLevel>();

            // Check services
            var runningServices = ServiceController.GetServices();
            foreach (var service in runningServices)
            {
                if (service.Status != ServiceControllerStatus.Stopped &&
                    service.Status != ServiceControllerStatus.Paused &&
                    Interference.ContainsKey(service.ServiceName))
                {
                    var level = Interference[service.ServiceName];

                    if ((level & InterferenceLevel.Service) == InterferenceLevel.Service)
                    {
                        found.Add(service.ServiceName, level);
                    }
                }
            }

            // Check processes
            var runningProcesses = Process.GetProcesses();
            foreach (var process in runningProcesses)
            {
                if (Interference.ContainsKey(process.ProcessName))
                {
                    var level = Interference[process.ProcessName];

                    if ((level & InterferenceLevel.Process) == InterferenceLevel.Process)
                    {
                        found.Add(process.ProcessName, level);
                    }
                }
            }

            return found;
        }

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

        internal static void DisableMceServices()
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
                Log.Warn(ex);
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
                Log.Warn(ex);
            }
        }

        internal static bool FindDevice(out Guid deviceGuid, out string devicePath)
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
                Log.Warn(ex);
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
                Log.Warn(ex);
            }

            return false;
        }

        #endregion
    }
}
