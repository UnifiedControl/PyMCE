using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using PyMCE.Core.Device;

namespace PyMCE_Debug.Managers
{
    public class LocalManager : INotifyPropertyChanged
    {
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                FirePropertyChanged("Status");
            }
        }

        public string ReceivingStatus
        {
            get { return _receivingStatus; }
            set
            {
                _receivingStatus = value;
                FirePropertyChanged("ReceivingStatus");
            }
        }

        public Transceiver Transceiver
        {
            get { return _transceiver; }
            set { _transceiver = value; }
        }
        
        public bool IsRunning
        {
            get { return _transceiver.CurrentRunningState == RunningState.Started; }
        }

        private string _status = "Idle";
        private string _receivingStatus = "";
        private Transceiver _transceiver;

        public LocalManager()
        {
            _transceiver = new Transceiver();
            _transceiver.CodeReceived += _transceiver_CodeReceived;
            _transceiver.StateChanged += _transceiver_StateChanged;
        }

        private void _transceiver_CodeReceived(object sender, CodeReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _transceiver_StateChanged(object sender, StateChangedEventArgs e)
        {
            Status = e.RunningState.ToString();
            ReceivingStatus = e.ReceivingState != ReceivingState.None ? e.ReceivingState.ToString() : "";

            Console.WriteLine(IsRunning);
            FirePropertyChanged("IsRunning"); // IsRunning automatically updates, fire a property changed event though.
        }

        public void Start()
        {
            _transceiver.Start();
        }

        public void Stop()
        {
            _transceiver.Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
