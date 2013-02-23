using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PyMCE_Debug.Managers
{
    public class ServiceManager : INotifyPropertyChanged
    {
        public string Status
        {
            get { return "Idle"; }
        }

        public string ReceivingStatus
        {
            get { return ""; }
        }

        public bool ControlsEnabled
        {
            get { return false; }
        }

        public ServiceManager()
        {

        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
