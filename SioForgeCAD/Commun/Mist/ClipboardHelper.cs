﻿using System;
using System.Runtime.InteropServices;

namespace SioForgeCAD.Commun.Mist
{
    public static class ClipboardHelper
    {
        private const uint GMEM_MOVEABLE = 0x0002;

        public static bool SetRawDataToClipboard(string Format, byte[] EPS, bool Append = false)
        {
            uint cfEps = User32PInvoke.RegisterClipboardFormat(Format);

            if (!User32PInvoke.OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            try
            {
                if (Append && !User32PInvoke.EmptyClipboard())
                {
                    return false;
                }

                UIntPtr size = (UIntPtr)EPS.Length;
                IntPtr hGlobal = User32PInvoke.GlobalAlloc(GMEM_MOVEABLE, size);
                if (hGlobal == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr ptr = User32PInvoke.GlobalLock(hGlobal);
                if (ptr == IntPtr.Zero)
                {
                    return false;
                }

                Marshal.Copy(EPS, 0, ptr, EPS.Length);
                User32PInvoke.GlobalUnlock(hGlobal);

                if (User32PInvoke.SetClipboardData(cfEps, hGlobal) == IntPtr.Zero)
                {
                    return false;
                }
            }
            finally
            {
                User32PInvoke.CloseClipboard();
            }

            return true;
        }
    }
}
