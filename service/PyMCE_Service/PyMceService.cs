using System.ServiceProcess;

namespace PyMCE_Service
{
    public partial class PyMceService : ServiceBase
    {
        public PyMceService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }
    }
}
