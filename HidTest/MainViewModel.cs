using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidTest;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public TestSettingClass testSetting = new();
    ConsoleControl.WPF.ConsoleControl consoleControl;

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
    string hidInputString = "";
    [ObservableProperty]
    string usbStatusString = "Disconnected";
    [ObservableProperty]
    Brush usbStatusColor;
    [ObservableProperty]
    public bool isHidInputText = true;
    [ObservableProperty]
    public bool isHidOutputText = true;
    [ObservableProperty]
    public bool isSendCommand = true;
    partial void OnIsSendCommandChanged(bool value)
    {
        if (value)
        {
            IsHidOutputText = value;
        }
    }

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
        if (IsHidInputText)
        {
            HidInputString += Encoding.ASCII.GetString(bytes, 0, length);
        }
        else
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                sb.AppendFormat("{0:x2} ", b);
            }

            HidInputString += sb.ToString();
        }

        App.mainwindow.HidInputTextBox.Dispatcher.Invoke(() =>
        {
            App.mainwindow.HidInputTextBox.ScrollToEnd();
        });

    }
    #endregion

    [RelayCommand]
    public async void SendHidOutReport(object param)
    {
        if (!myHidDevice.isConnected) { return; }

        if (IsSendCommand)
        {
            await myHidDevice.WriteCommandAsync(HidOutputString);
        }
        else
        {
            if (IsHidOutputText)
            {
                await myHidDevice.WriteStringAsync(HidOutputString);
            }
        }
    }

    [RelayCommand]
    public void LoadHidOutputString(object param)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();

        // Set filter options to allow only text files
        openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
        openFileDialog.FilterIndex = 1;

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;

            // Read the text from the file
            HidOutputString = File.ReadAllText(filePath, Encoding.UTF8);

        }
    }

    [RelayCommand]
    public void ClearHidInputString(object param)
    {
        HidInputString = "";
    }
}

