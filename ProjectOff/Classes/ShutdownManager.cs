using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectOff.Classes
{
    public static class ShutdownManager
    {
        public static void Shutdown()
        {
            Process.Start("shutdown", "/s /t 0");
        }

        public static void Restart()
        {
            Process.Start("shutdown", "/r /t 0");
        }

        public static void Sleep()
        {
            Application.SetSuspendState(PowerState.Suspend, true, true);
        }
    }
}
