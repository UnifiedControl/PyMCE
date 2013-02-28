using System;
using PyMCE.Core.Utils;
using System.IO;
using System.IO.Pipes;

namespace PyMCE.Core.Device.Agent
{
    class NamedPipeServer : Direct
    {
        #region Constants

        private const int ReadBufferSize = 512;

        #endregion

        #region Variables

        private byte[] _buffer;
        private NamedPipeServerStream _pipe;

        #endregion

        #region Properties

        public override string PipeName { get; set; }

        #endregion

        public override void Start(InterferenceLevel[] ignore = null, bool disableMceServices = true)
        {
            if(PipeName == null)
                throw new InvalidOperationException("PipeName is required");

            base.Start(ignore, disableMceServices);

            BeginWaitForConnection();
        }

        private void BeginWaitForConnection()
        {
            Log.Trace("BeginWaitForConnection()");

            _pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1,
                                              PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            _pipe.BeginWaitForConnection(EndWaitForConnection, null);
        }

        private void EndWaitForConnection(IAsyncResult result)
        {
            Log.Trace("EndWaitForConnection()");

            _pipe.EndWaitForConnection(result);
            BeginRead();
        }

        private void BeginRead()
        {
            Log.Trace("BeginRead()");

            if (_pipe.IsConnected)
            {
                _buffer = new byte[ReadBufferSize];
                _pipe.BeginRead(_buffer, 0, ReadBufferSize, EndRead, null);
            }
            else
            {
                _pipe.Close();
                BeginWaitForConnection();
            }
        }

        private void EndRead(IAsyncResult result)
        {
            Log.Trace("EndRead()");

            var bytesRead = _pipe.EndRead(result);
            Log.Debug("bytesRead: {0}, IsMessageComplete: {1}", bytesRead, _pipe.IsMessageComplete);

            BeginRead();
        }
    }
}
