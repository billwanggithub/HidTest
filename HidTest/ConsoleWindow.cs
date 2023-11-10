using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

public static class ConsoleWindow
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("Kernel32")]
    public static extern void AllocConsole();

    [DllImport("Kernel32")]
    public static extern void FreeConsole();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public static void SetConsoleOwnerWindow(Window ownerWindow)
    {
        var consoleWindowHandle = GetConsoleWindow();
        GetWindowThreadProcessId(consoleWindowHandle, out uint processId);
        var process = Process.GetProcessById((int)processId);
        var ownerWindowHandle = (ownerWindow != null) ? new System.Windows.Interop.WindowInteropHelper(ownerWindow).Handle : IntPtr.Zero;

        SetParent(consoleWindowHandle, ownerWindowHandle);
    }

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    public static IntPtr handle = IntPtr.Zero;

    public static IntPtr Show()
    {
        handle = GetConsoleWindow();
        if (handle == IntPtr.Zero)
        {
            AllocConsole();
            handle = GetConsoleWindow();
        }
        else
            ShowWindow(handle, SW_SHOW);
        SetForegroundWindow(handle);
        return handle;
    }

    public static void Hide()
    {
        handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }
}