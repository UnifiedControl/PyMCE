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

using PyMCE.Core.Device;
using System.Windows;
using PyMCE_Debug.Managers;

namespace PyMCE_Debug
{
    public partial class MainWindow : Window
    {
        public LocalManager Local { get; set; }
        public ServiceManager Service { get; set; }

        public MainWindow()
        {
            Local = new LocalManager();
            Service = new ServiceManager();

            DataContext = this;

            InitializeComponent();
        }

        private void LocalStart(object sender, RoutedEventArgs e)
        {
            Local.Start();
        }

        private void LocalStop(object sender, RoutedEventArgs e)
        {
            Local.Stop();
        }

        private void ServiceStart(object sender, RoutedEventArgs e)
        {
            Service.Start();
        }

        private void ServiceStop(object sender, RoutedEventArgs e)
        {
            Service.Stop();
        }
    }
}
