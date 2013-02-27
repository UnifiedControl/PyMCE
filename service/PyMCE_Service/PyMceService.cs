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

using System.Diagnostics;
using System.IO.Pipes;
using System.ServiceProcess;
using PyMCE.Core.Device;

namespace PyMCE_Service
{
    public partial class PyMceService : ServiceBase
    {
        public EventLog Logger { get; private set; }

        private readonly NamedPipeServerStream _pipe;
        private readonly Transceiver _transceiver;

        public PyMceService()
        {
            InitializeComponent();

            // Setup event logger
            Logger = new EventLog();
            if (!EventLog.SourceExists(ServiceName))
            {
                EventLog.CreateEventSource(ServiceName, "Application");
            }
            Logger.Source = ServiceName;
            Logger.EnableRaisingEvents = true;
            Debug.Listeners.Add(new EventLogWriterTraceListener(Logger));

            Logger.WriteEntry("Constructed");

            // Create named pipe for IPC
            _pipe = new NamedPipeServerStream(ServiceName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            Logger.WriteEntry("Pipe Constructed");

            // Create the PyMCE Transceiver
            _transceiver = new Transceiver(TransceiverMode.PipeInput);
            _transceiver.SetPipe(_pipe);
            Logger.WriteEntry("Transceiver Constructed");
        }

        protected override void OnStart(string[] args)
        {
            Logger.WriteEntry("OnStart");

            _transceiver.Start();
        }

        protected override void OnStop()
        {
            Logger.WriteEntry("OnStop");

            _transceiver.Stop();
        }
    }
}
