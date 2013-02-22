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

        private string _status = "Idle";
        private Transceiver _transceiver;

        public LocalManager()
        {
            _transceiver = new Transceiver();
        }

        public void Start()
        {
            _transceiver.Start();
            Status = "Starting";
        }

        public void Stop()
        {
            _transceiver.Stop();
            Status = "Stopping";
        }

        public LearnStatus Learn(out byte[] data)
        {
            return _transceiver.Learn(out data);
        }

        public bool Transmit(string port, byte[] data)
        {
            return _transceiver.Transmit(port, data);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
