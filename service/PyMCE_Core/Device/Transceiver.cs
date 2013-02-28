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
using System.Collections.Generic;
using System.IO;
using PyMCE.Core.Utils;

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

    [Flags]
    public enum InterferenceLevel
    {
        Service = 1,
        Process = 2,

        Learn = 4,
        Transmit = 8,
        Receive = 16
    }

    public enum TransceiverMode
    {
        /// <summary>
        /// Directly connect to the MCE Device
        /// </summary>
        Direct,

        /// <summary>
        /// Pipe commands to the output steam
        /// </summary>
        NamedPipeClient,

        /// <summary>
        /// Wait for piped commands from the input stream
        /// </summary>
        NamedPipeServer
    }

    public enum RunningState
    {
        Unknown,

        Starting,
        Started,

        Stopping,
        Stopped
    }

    public enum ReceivingState
    {
        Unknown,

        Receiving,
        Learning
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
        public IRCode Code { get; private set; }

        internal LearnResult(LearnStatus status, IRCode code)
        {
            Status = status;
            Code = code;
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

    public class InterferenceException : Exception
    {
        public Dictionary<string, InterferenceLevel> Interference { get; private set; } 

        public InterferenceException(Dictionary<string, InterferenceLevel> interference, string message)
            : base(message)
        {
            Interference = interference;
        }
    }

    #endregion

    public class Transceiver
    {
        #region Variables

        private readonly TransceiverMode _currentMode = TransceiverMode.Direct;
        private Agent.AgentBase _agent = null;

        private bool _disableMceServices = true;

        #endregion

        #region Properties

        #region Configuration

        public InterferenceLevel[] InterferenceIgnore { get; set; }

        public bool DisableMceServices
        {
            get { return _disableMceServices; }
            set { _disableMceServices = value; }
        }

        #endregion

        #region NamedPipeClient, NamedPipeServer

        public string PipeName
        {
            get { return _agent.PipeName; }
            set { _agent.PipeName = value; }
        }

        #endregion

        public RunningState CurrentRunningState
        {
            get { return _agent.CurrentRunningState; }
        }

        public ReceivingState CurrentReceivingState
        {
            get { return _agent.CurrentReceivingState; }
        }

        #endregion

        #region Events

        public event CodeReceivedDelegate CodeReceived;
        public event StateChangedDelegate StateChanged;

        #endregion

        #region Constructor

        public Transceiver()
        {
            ConstructAgent();
        }

        public Transceiver(TransceiverMode mode)
        {
            _currentMode = mode;
            ConstructAgent();
        }

        private void ConstructAgent()
        {
            switch (_currentMode)
            {
                case TransceiverMode.Direct:
                    _agent = new Agent.Direct();
                    break;
                case TransceiverMode.NamedPipeClient:
                    _agent = new Agent.NamedPipeClient();
                    break;
                case TransceiverMode.NamedPipeServer:
                    _agent = new Agent.NamedPipeServer();
                    break;
            }

            // Setup callbacks
            _agent.StateChangedCallback = Agent_StateChangedCallback;
            _agent.CodeReceivedCallback = Agent_CodeReceivedCallback;
        }

        #endregion

        #region Public Methods

        #region Learn

        public LearnStatus Learn(out IRCode code)
        {
            return _agent.Learn(out code);
        }

        public LearnResult Learn()
        {
            IRCode code;
            var status = Learn(out code);

            return new LearnResult(status, code);
        }

        public void LearnAsync(LearnCompletedDelegate callback)
        {
            Log.Trace("LearnAsync()");
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

        public bool Transmit(string port, IRCode code)
        {
            return _agent.Transmit(port, code);
        }

        #endregion

        #region Control Methods

        public void Start()
        {
            _agent.Start(InterferenceIgnore, DisableMceServices);
        }

        public void Suspend()
        {
            _agent.Suspend();
        }

        public void Resume()
        {
            _agent.Resume();
        }

        public void Stop()
        {
            _agent.Stop();
        }

        #endregion

        #endregion

        #region Agent Callbacks

        private void Agent_CodeReceivedCallback(object sender, CodeReceivedEventArgs codeReceivedEventArgs)
        {
            if (CodeReceived != null)
                CodeReceived(sender, codeReceivedEventArgs);
        }

        private void Agent_StateChangedCallback(object sender, StateChangedEventArgs stateChangedEventArgs)
        {
            if (StateChanged != null)
                StateChanged(sender, stateChangedEventArgs);
        }

        #endregion
    }
}
