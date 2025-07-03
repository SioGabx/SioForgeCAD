using System;
using System.Runtime.InteropServices;

namespace SioForgeCAD.Commun.Mist
{
    public static class User32PInvoke
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static bool SetAsForeground(this IntPtr hWnd)
        {
            return SetForegroundWindow(hWnd);
        }
        public static bool SetAsForeground(this Autodesk.AutoCAD.Windows.Window win)
        {
            return SetAsForeground(win.Handle);
        }

        [DllImport("user32.dll")]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        public static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        public static extern uint RegisterClipboardFormat(string lpszFormat);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        public static extern bool GlobalUnlock(IntPtr hMem);

    }
}
