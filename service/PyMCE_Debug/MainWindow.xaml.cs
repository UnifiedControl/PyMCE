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

using System.Windows.Controls;
using PyMCE.Core.Device;
using System.Windows;
using PyMCE_Debug.Managers;
using System.Text;
using System.Collections.Generic;
using System;

namespace PyMCE_Debug
{
    public partial class MainWindow : Window
    {
        public LocalManager Local { get; set; }
        public ServiceManager Service { get; set; }

        private Dictionary<string, string> _prontoFormats = new Dictionary<string, string>()
                                                                {
                                                                    {"0000", "Learned (modulated)"},
                                                                    {"0100", "Learned (unmodulated)"},
                                                                    {"5000", "RC5"},
                                                                    {"5001", "RC5x"},
                                                                    {"6000", "RC6"},
                                                                    {"6001", "RC5a"},
                                                                };

        public MainWindow()
        {
            Local = new LocalManager();
            Service = new ServiceManager();

            DataContext = this;

            InitializeComponent();

            UpdateCodeInfo();
        }

        #region Event Handlers

        #region Local

        private void LocalStart(object sender, RoutedEventArgs e)
        {
            Local.Start();
        }

        private void LocalStop(object sender, RoutedEventArgs e)
        {
            Local.Stop();
        }

        private void LocalLearn(object sender, RoutedEventArgs e)
        {
            byte[] code;
            var status = Local.Transceiver.Learn(out code);

            if (status == LearnStatus.Success)
            {
                Code.Text = Encoding.ASCII.GetString(code);
            }
            else
            {
                Code.Text = "";
            }
        }

        private void LocalTransmit(object sender, RoutedEventArgs e)
        {
            var code = Encoding.ASCII.GetBytes(Code.Text);

            Local.Transceiver.Transmit("", code);
        }

        #endregion

        #region Service

        private void ServiceStart(object sender, RoutedEventArgs e)
        {
            Service.Start();
        }

        private void ServiceStop(object sender, RoutedEventArgs e)
        {
            Service.Stop();
        }

        private void ServiceLearn(object sender, RoutedEventArgs e)
        {
            Local.Stop();
        }

        private void ServiceTransmit(object sender, RoutedEventArgs e)
        {
            Local.Stop();
        }

        #endregion

        private void Code_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCodeInfo();
        }

        #endregion

        private void UpdateCodeInfo()
        {
            // Reset values to nothing
            CodeFormat.Content = "";
            CodeCarrier.Content = "";

            CodeOncePairs.Content = "";
            CodeRepeatPairs.Content = "";

            // Update with new data
            var prontoWords = Code.Text.Split(' ');

            for (var wi = 0; wi < prontoWords.Length; wi++)
            {
                var word = prontoWords[wi];

                if (word.Length == 4) // Pronto words are 4 characters long
                {
                    switch (wi)
                    {
                        case 0: // Format
                            if(_prontoFormats.ContainsKey(word))
                                CodeFormat.Content = word + " - " + _prontoFormats[word];
                            break;

                        case 1: // Carrier Frequency
                            var carrierDec = Convert.ToInt32(word, 16);
                            var carrierFreq = 1000000/(carrierDec*0.241246);
                            CodeCarrier.Content = string.Format("{0:G} Hz", (int)carrierFreq);
                            break;

                        case 2: // Once Pairs
                            var onceDec = Convert.ToInt32(word, 16);
                            CodeOncePairs.Content = string.Format("{0:G} (0x{0:X})", (int) onceDec);
                            break;

                        case 3: // Repeat Pairs
                            var repeatDec = Convert.ToInt32(word, 16);
                            CodeRepeatPairs.Content = string.Format("{0:G} (0x{0:X})", (int) repeatDec);
                            break;
                    }
                }
            }
        }
    }
}
