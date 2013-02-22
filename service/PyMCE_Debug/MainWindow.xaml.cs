using PyMCE_Core.Device;
using System.Windows;

namespace PyMCE_Debug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var transceiver = new Transceiver();
            transceiver.Start();
        }
    }
}
