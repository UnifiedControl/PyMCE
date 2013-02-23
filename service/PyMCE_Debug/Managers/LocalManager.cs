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
using System.ComponentModel;
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

        public bool ControlsEnabled
        {
            get
            {
                if (_transceiver.CurrentRunningState != RunningState.Started)
                    return false;
                return _transceiver.CurrentReceivingState == ReceivingState.Receiving;
            }
        }

        private string _status = "Idle";
        private string _receivingStatus = "";
        private readonly MainWindow _mainWindow;
        private Transceiver _transceiver;

        public LocalManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            _transceiver = new Transceiver();
            _transceiver.CodeReceived += _transceiver_CodeReceived;
            _transceiver.StateChanged += _transceiver_StateChanged;
        }

        #region Event Handlers

        private void _transceiver_CodeReceived(object sender, CodeReceivedEventArgs e)
        {
            _mainWindow.Dispatcher.BeginInvoke((Action) (() =>
                                                         _mainWindow.LogView.Items.Insert(0, 
                                                             Code.FromIRCode(_mainWindow.LogView.Items.Count + 1, e.Code))));
        }

        private void _transceiver_StateChanged(object sender, StateChangedEventArgs e)
        {
            Status = e.RunningState.ToString();
            ReceivingStatus = e.ReceivingState != ReceivingState.None ? e.ReceivingState.ToString() : "";

            FirePropertyChanged("ControlsEnabled");
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            _transceiver.Start();
        }

        public void Stop()
        {
            _transceiver.Stop();
        }

        public void Learn(LearnCompletedDelegate callback)
        {
            _transceiver.LearnAsync(callback);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
