using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidTest;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

    #region HID
    MyHidClass myHidDevice;
    [ObservableProperty]
    string hidOutputString = "*IDN?\n";
    [ObservableProperty]
    string hidInputString = "";
    partial void OnHidInputStringChanged(string value)
    {
        App.mainwindow.HidInputTextBox.Dispatcher.Invoke(() =>
        {
            App.mainwindow.HidInputTextBox.ScrollToEnd();
        });
    }
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
        DeviceListChangedHandler(null, null); // Check USB
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
            HidInputString += ReplaceNonPrintableChar(Encoding.ASCII.GetString(bytes, 0, length));
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

        WriteConsole("HID Report Received\n");
    }
    #endregion

    #region Functions
    public void WriteConsole(string message, Color? color = null)
    {
        consoleControl.WriteOutput(message, color ?? Colors.White);
        App.mainwindow.ConsoleControlScrollViewer.Dispatcher.Invoke(() =>
        {
            App.mainwindow.ConsoleControlScrollViewer.ScrollToEnd();
        });
    }
    string ReplaceNonPrintableChar(string input)
    {
        string output = "";

        foreach (char c in input)
        {
            //if (char.IsControl(c) && c != '\n')
            if (!char.IsLetterOrDigit(c))
            {
                // Replace non-printable character with a specific character
                output += '.';
            }
            else
            {
                output += c;
            }
        }

        return output;
    }
    #endregion

    #region Relay Commands
    [RelayCommand]
    public async Task SendHidOutReportAsync(object param)
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
    public void SaveHidOutputString(object param)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();

        // Set filter options to allow only text files
        saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
        saveFileDialog.FilterIndex = 1;

        if (saveFileDialog.ShowDialog() == true)
        {
            string filePath = saveFileDialog.FileName;

            // Get the text content that you want to save (from your ViewModel or elsewhere)
            string textToSave = HidOutputString;

            // Save the text to the selected file
            File.WriteAllText(filePath, textToSave);

            // Show a message box to indicate that the file was saved successfully
            MessageBox.Show("File saved successfully!");
        }
    }

    [RelayCommand]
    public void ClearHidInputString(object param)
    {
        HidInputString = "";
    }

    #endregion

}

