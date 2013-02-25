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
using PyMCE.Core.Infrared;
using System.Diagnostics;

namespace PyMCE_Debug
{
    public partial class MainWindow : Window
    {
        public LocalManager Local { get; set; }
        public ServiceManager Service { get; set; }

        public MainWindow()
        {
            Local = new LocalManager(this);
            Service = new ServiceManager(this);

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
            Local.Learn(delegate(LearnResult result)
                            {
                                if (result.Status == LearnStatus.Success)
                                {
                                    Dispatcher.BeginInvoke(
                                        (Action) (() =>
                                                      {
                                                          CodeString.Text = result.Code.ToProntoString();
                                                          LogView.Items.Insert(0,
                                                                               Code.FromIRCode(
                                                                                   LogView.Items.Count + 1,
                                                                                   result.Code));
                                                      }));
                                }
                                else
                                {
                                    Debug.WriteLine("Learning Failed: " + result.Status);
                                }
                            });
        }

        private void LocalTransmit(object sender, RoutedEventArgs e)
        {
            var code = Encoding.ASCII.GetBytes(CodeString.Text);

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

        private void CodeString_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCodeInfo();
        }

        private void LogView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;

            var code = (Code) e.AddedItems[0];
            CodeString.Text = code.ProntoCode;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            LogView.Items.Clear();
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
            var prontoWords = CodeString.Text.Split(' ');

            for (var wi = 0; wi < prontoWords.Length; wi++)
            {
                var word = prontoWords[wi];

                if (word.Length == 4) // Pronto words are 4 characters long
                {
                    switch (wi)
                    {
                        case 0: // Format
                            var format = IRFormat.FromProntoWord(word);
                            CodeFormat.Content = format.Format + " (" + format.Word + ")";
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
