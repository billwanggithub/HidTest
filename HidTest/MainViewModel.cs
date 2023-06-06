using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidTest;
using System;
using System.Text;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public TestSettingClass testSetting = new();
    ConsoleControl.WPF.ConsoleControl consoleControl;

    [ObservableProperty]
    public bool isHidInputText = true;
    [ObservableProperty]
    public bool isHidOutputText = true;

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

    #region HID
    MyHidClass myHidDevice;
    [ObservableProperty]
    string hidOutputString = "*IDN?\n";
    [ObservableProperty]
    string hidInputString = "Input";
    [ObservableProperty]
    string usbStatusString = "Disconnected";
    [ObservableProperty]
    Brush usbStatusColor;

    public void AutoConnect()
    {
        WriteConsole("Start USB Auto Detection\n");

        UsbStatusColor = Brushes.Red;
        myHidDevice = new MyHidClass(TestSetting.UsbVid, TestSetting.UsbPid);
        myHidDevice.InputReportReceived = InputReportReceived;
        MyHidClass.localDeviceList.Changed += DeviceListChangedHandler;
        DeviceListChangedHandler(null, null);
    }
    void DeviceListChangedHandler(object? sender, EventArgs e)
    {
        //WriteConsole("Device list changed.\n");
        myHidDevice.CheckHidDevice();

        if (myHidDevice.hidDevice == null)
        {
            DeviceRemoved();
        }
        else
        {
            DeviceConnected();
        }
    }

    void DeviceConnected()
    {
        WriteConsole($"Device Found VID = 0X{myHidDevice.venderId:X4}, PID = 0X{myHidDevice.productId:X4}\n", Colors.LightGreen);
        UsbStatusString = "Connected";
        UsbStatusColor = Brushes.DarkGreen;
        myHidDevice.OpenDevice();
    }
    void DeviceRemoved()
    {
        WriteConsole($"Device not Found\n", Colors.Red);
        UsbStatusString = "DisConnected";
        UsbStatusColor = Brushes.Red;
    }

    void InputReportReceived(byte[] bytes, int length)
    {
        if (!myHidDevice.isConnected) { return; }
        WriteConsole(Encoding.ASCII.GetString(bytes, 0, length) + Environment.NewLine, Colors.LightBlue);
    }
    #endregion

    [RelayCommand]
    public async void SendHidOutReport(object param)
    {
        if (!myHidDevice.isConnected) { return; }
        await myHidDevice.WriteCommandAsync("*IDN?\n");
    }


}

