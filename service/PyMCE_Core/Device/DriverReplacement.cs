using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PyMCE_Core.Infrared;

namespace PyMCE_Core.Device
{
    internal class DriverReplacement : Driver
    {
        #region Constructor

        public DriverReplacement(Guid deviceGuid, string devicePath)
            : base(deviceGuid, devicePath)
        {

        }

        #endregion

        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        public override void Suspend()
        {
            throw new NotImplementedException();
        }

        public override void Resume()
        {
            throw new NotImplementedException();
        }

        public override Transceiver.LearnStatus Learn(int learnTimeout, out IRCode learned)
        {
            throw new NotImplementedException();
        }

        public override void Send(IRCode code, int port)
        {
            throw new NotImplementedException();
        }
    }
}
