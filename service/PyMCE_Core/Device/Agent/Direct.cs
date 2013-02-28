using PyMCE.Core.Infrared;
using PyMCE.Core.Utils;
using System;

namespace PyMCE.Core.Device.Agent
{
    class Direct : AgentBase
    {
        #region Variables

        private readonly object _learnLock = new object();

        #endregion

        #region Public Methods

        public override LearnStatus Learn(out IRCode code)
        {
            Log.Trace("Learn()");
            LearnStatus status;

            lock (_learnLock)
            {
                status = Driver.Learn(LearnTimeout, out code);
            }

            return status;
        }

        public override bool Transmit(string port, IRCode code)
        {
            Log.Trace("Transmit()");

            var blasterPort = BlasterPort.Both;
            try
            {
                blasterPort = (BlasterPort)Enum.Parse(typeof(BlasterPort), port, true);
            }
            catch (Exception)
            {
                Log.Warn("Invalid Blaster Port ({0}), using default {1}", port, blasterPort);
            }

            if (code == null)
                throw new ArgumentException("Invalid IR Command data", "code");

            Driver.Send(code, (int)blasterPort);

            return true;
        }

        #region Control

        public override void Start(InterferenceLevel[] ignore = null,
            bool disableMceServices = true)
        {
            Log.Trace("Start()");

            if(StateChangedCallback == null || CodeReceivedCallback == null)
                throw new InvalidOperationException("Agent callbacks have not been set, unable to start");

            if (Driver != null)
                throw new InvalidOperationException("MicrosoftMceTransceiver already started");

            if (disableMceServices)
                DisableMceServices();

            var interference = InterferenceCheck();
            var interferenceError = false;
            var interferenceErrorMessage = "The following programs/services have been found to cause interference and should be closed: ";

            foreach (var item in interference)
            {
                var itemName = item.Key;
                var itemError = true;
                if (ignore != null)
                {
                    foreach (var ignoreLevel in ignore)
                    {
                        if ((item.Value & ignoreLevel) == ignoreLevel)
                        {
                            itemError = false;
                            itemName = "";
                        }
                    }
                }
                interferenceErrorMessage += itemName + ", ";
                if (!interferenceError) interferenceError = itemError;
            }

            if (interferenceError)
            {
                throw new InterferenceException(interference,
                    interferenceErrorMessage.Substring(0, interferenceErrorMessage.Length - 2));
            }

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

            Driver = newDriver;
            Driver.StateChangedCallback = StateChangedCallback;
            Driver.CodeReceivedCallback = CodeReceivedCallback;
            Driver.Start();
        }

        public override void Suspend()
        {
            Log.Trace("Suspend()");
            if (Driver != null)
                Driver.Suspend();
        }

        public override void Resume()
        {
            Log.Trace("Resume()");
            if (Driver != null)
                Driver.Resume();
        }

        public override void Stop()
        {
            if (Driver == null) return;

            Log.Trace("Stop()");

            try
            {
                Driver.Stop();
            }
            finally
            {
                Driver = null;
            }
        }

        #endregion

        #endregion
    }
}
