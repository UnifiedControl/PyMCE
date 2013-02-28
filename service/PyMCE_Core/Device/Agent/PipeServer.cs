
using PyMCE.Core.Infrared;

namespace PyMCE.Core.Device.Agent
{
    class PipeServer : AgentBase
    {
        #region Public Methods

        public override LearnStatus Learn(out IRCode code)
        {
            throw new System.NotImplementedException();
        }

        public override bool Transmit(string port, IRCode code)
        {
            throw new System.NotImplementedException();
        }

        #region Control

        public override void Start(InterferenceLevel[] ignore = null,
            bool disableMceServices = true)
        {
            throw new System.NotImplementedException();
        }

        public override void Suspend()
        {
            throw new System.NotImplementedException();
        }

        public override void Resume()
        {
            throw new System.NotImplementedException();
        }

        public override void Stop()
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #endregion
    }
}
