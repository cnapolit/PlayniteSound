using System;
using System.Runtime.InteropServices;

namespace PlayniteSounds.Common.Imports
{
    internal static class User32
    {

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
}
