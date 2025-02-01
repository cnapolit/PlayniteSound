using System;
using System.Runtime.InteropServices;

namespace PlayniteSounds.Common.Imports;

public static class User32
{

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}