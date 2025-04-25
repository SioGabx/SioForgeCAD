using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Mist
{
    public static class User32PInvoke
    {
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public static bool SetAsForeground(this IntPtr hWnd)
        {
            return SetForegroundWindow(hWnd);
        }
        public static bool SetAsForeground(this Autodesk.AutoCAD.Windows.Window win)
        {
            return SetAsForeground(win.Handle);
        }

    }
}
