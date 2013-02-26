using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PyMCE_Service
{
    class EventLogWriterTraceListener : TraceListener
    {
        private EventLog _eventLog;
        private string _buffer;

        public EventLogWriterTraceListener(EventLog eventLog)
        {
            _eventLog = eventLog;
        }

        public override void Write(string message)
        {
            _buffer += message;
        }

        public override void WriteLine(string message)
        {
            _eventLog.WriteEntry(_buffer + message);
            _buffer = "";
        }
    }
}
