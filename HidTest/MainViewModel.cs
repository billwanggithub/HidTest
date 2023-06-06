using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidSharp;
using HidSharp.Utility;
using HidTest;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

public partial class MainViewModel : ObservableObject
{
    ConsoleControl.WPF.ConsoleControl consoleControl;

    [ObservableProperty]
    public TestSettingClass testSetting = new();

    public MainViewModel()
    {
        consoleControl = App.mainwindow.consoleCOntrol;
        WriteConsole("Test HID\n", color: Colors.White);
        AutoConnect();
    }


    public void WriteConsole(string message, Color? color = null)
    {
        consoleControl.WriteOutput(message, color ?? Colors.White);
    }

    [RelayCommand]
    public void ListHidDevice(object param)
    {
    }

    [ObservableProperty]
    string hidOutReportString = "Input Hex string here";

    DeviceList list;
    HidDevice[] hidDeviceList;
    HidDevice hidDevice = null;

    [ObservableProperty]
    string usbStatusString = "Disconnected";
    [ObservableProperty]
    Brush usbStatusColor;

    [RelayCommand]
    public void ConnectHidDevice(object param)
    {
    }

    public void AutoConnect()
    {
        WriteConsole("Start USB Auto Detection\n");

        HidSharpDiagnostics.EnableTracing = true;
        HidSharpDiagnostics.PerformStrictChecks = true;

        UsbStatusColor = Brushes.Red;

        list = DeviceList.Local;
        list.Changed += DeviceListChangedHandler;

        hidDeviceList = list.GetHidDevices().ToArray();

        Task.Run(async () =>
        {
            list.GetHidDevices();
            await Task.Delay(1000);
        });
    }

    HidDevice CheckHidDevice()
    {
        hidDeviceList = list.GetHidDevices().ToArray();
        foreach (HidDevice dev in hidDeviceList)
        {
            if ((dev.VendorID == 0x0483) && (dev.ProductID == 0x5750))
            {
                TestSetting.UsbVid = (uint)dev.VendorID;
                TestSetting.UsbPid = (uint)dev.ProductID;

                return dev;
            }
        }
        return null;
    }

    void DeviceListChangedHandler(object? sender, EventArgs e)
    {
        WriteConsole("Device list changed.\n");
        hidDevice = CheckHidDevice();
        if (hidDevice == null)
        {
            WriteConsole($"Device not Found\n", Colors.Red);
            UsbStatusString = "DisConnected";
            UsbStatusColor = Brushes.Red;
        }
        else
        {
            WriteConsole($"Device Found VID = 0X{hidDevice.VendorID:X4}, PID = 0X{hidDevice.ProductID:X4}\n", Colors.LightGreen);
            UsbStatusString = "Connected";
            UsbStatusColor = Brushes.DarkGreen;
        }
    }
}

