using System;
using System.Runtime.InteropServices;

namespace AlbionDpsMeter.Startup;

/// <summary>
/// Pure Win32 MessageBox wrapper. Used by the pre-launch validator because it
/// must work even when the Windows App Runtime is missing (i.e. before any
/// WinUI / WinRT call has been made).
/// </summary>
internal static class NativeMessageBox
{
    private const uint MB_OK = 0x0;
    private const uint MB_YESNO = 0x4;
    private const uint MB_ICONERROR = 0x10;
    private const uint MB_TOPMOST = 0x40000;
    private const uint MB_SETFOREGROUND = 0x10000;

    private const int IDYES = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>
    /// Shows a modal Yes/No error dialog. Returns true only if the user
    /// clicked Yes; closing the dialog or clicking No returns false.
    /// </summary>
    public static bool ShowYesNo(string text, string caption)
    {
        int result = MessageBoxW(
            IntPtr.Zero,
            text,
            caption,
            MB_YESNO | MB_ICONERROR | MB_TOPMOST | MB_SETFOREGROUND);
        return result == IDYES;
    }
}
