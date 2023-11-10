using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidTest;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        ConsoleWindow.Show();
        IntPtr hMenu = WIN32Commands.GetSystemMenu(ConsoleWindow.handle, false);

        consoleControl = App.mainwindow.consoleCOntrol;
        WriteConsole("Test HID\n", color: Colors.White);
        AutoConnect();
    }

    #region HID
    [ObservableProperty]
    MyHidClass myHidDevice;
    [ObservableProperty]
    string hidOutputString = "*IDN?\nAPP_BUID?\n";
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
        MyHidDevice = new MyHidClass(TestSetting.UsbVid, TestSetting.UsbPid);
        MyHidDevice.InputReportReceived = InputReportReceived;
        MyHidClass.localDeviceList.Changed += DeviceListChangedHandler;
        DeviceListChangedHandler(null, null); // Check USB
    }
    void DeviceListChangedHandler(object sender, EventArgs e)
    {
        //WriteConsole("Device list changed.\n");
        MyHidDevice.CheckHidDevice();

        if (MyHidDevice.hidDevice == null)
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
        WriteConsole($"Device Found VID = 0X{MyHidDevice.venderId:X4}, PID = 0X{MyHidDevice.productId:X4}\n", Colors.LightGreen);
        UsbStatusString = "Connected";
        UsbStatusColor = Brushes.DarkGreen;
        MyHidDevice.OpenDevice();
    }
    void DeviceRemoved()
    {
        WriteConsole($"Device not Found\n", Colors.Red);
        UsbStatusString = "DisConnected";
        UsbStatusColor = Brushes.Red;
    }

    void InputReportReceived(byte[] bytes, int length)
    {
        if (!MyHidDevice.IsConnected) { return; }

        ///* remark here for FG test */
        //if (IsHidInputText)
        //{
        //    HidInputString += ReplaceNonPrintableChar(Encoding.ASCII.GetString(bytes, 0, length));
        //}
        //else
        //{
        //    StringBuilder sb = new StringBuilder(bytes.Length * 2);

        //    foreach (byte b in bytes)
        //    {
        //        sb.AppendFormat("{0:x2} ", b);
        //    }

        //    HidInputString += sb.ToString();
        //}
        //WriteConsole("HID Report Received\n");

        ParseHidReport(bytes);
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
        if (!MyHidDevice.IsConnected) { return; }

        string command = HidOutputString.Replace("\r\n", "\n");
        if (IsSendCommand)
        {
            await MyHidDevice.WriteCommandAsync(command, 10);
        }
        else
        {
            if (IsHidOutputText)
            {
                await MyHidDevice.WriteStringAsync(command, 10);
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
            HidOutputString = File.ReadAllText(filePath, Encoding.UTF8).Replace("\r\n", "\n");

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
            string textToSave = HidOutputString.Replace("\r\n", "\n");

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

    [RelayCommand]
    public async Task Test1(object param)
    {
        oldTime = 0;
        Fg1Counter = 0;
        fgMissingCount = 0;
        measurementData = new();
        measurementQueue = new();

        string command = File.ReadAllText("Commands\\test_capture.txt");
        await MyHidDevice.WriteCommandAsync(command);

        // Save measurement data
        MeasurementData dataRead;
        File.WriteAllText("Commands\\measurements.csv", $"period, width, busVoltage, shuntVoltage, time_ms, time_diff\n");

        cs = new CancellationTokenSource();
        ct = cs.Token;

        await Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                while (measurementQueue.Count > 0)
                {
                    if (measurementQueue.TryDequeue(out dataRead))
                    {
                        //await File.AppendAllTextAsync("Commands\\measurements.csv",
                        //    $"{dataRead.period}, {dataRead.width}, {dataRead.busVoltage}, {dataRead.shuntVoltage}, {dataRead.time_ms}, {dataRead.time_ms - timeOld}\n");
                        //timeOld = dataRead.time_ms;
                    }
                }
                await Task.Delay(1000);
            }

            WriteConsole("Stop save file task\n");
            WriteConsole($"measurement remain {measurementQueue.Count} counts\n");
            while (measurementQueue.Count > 0)
            {
                if (measurementQueue.TryDequeue(out dataRead))
                {
                    //await File.AppendAllTextAsync("Commands\\measurements.csv",
                    //    $"{dataRead.period}, {dataRead.width}, {dataRead.busVoltage}, {dataRead.shuntVoltage}, {dataRead.time_ms}, {dataRead.time_ms - timeOld}\n");
                    //timeOld = dataRead.time_ms;
                    //WriteConsole($"time_ms = {dataRead.time_ms}\n");
                }
            }
            MyHidDevice.WriteCommand("DEBUG:rc\n");
            WriteConsole($"measurement remain {measurementQueue.Count} counts\n");
            uint diff = measurementData.time_ms - Fg1Counter;
            string message = $"{DateTime.Now} received = {Fg1Counter} send= {measurementData.time_ms} diff =  {diff} " +
                $"error rate = {diff * 100.0 / measurementData.time_ms}%\n";
            WriteConsole(message);
            WriteConsole("Exit save file task\n");

        }, ct).ConfigureAwait(false);

    }
    [RelayCommand]
    public async Task Test2(object param)
    {
        string command = File.ReadAllText("Commands\\test_fg_off.txt");
        await MyHidDevice.WriteCommandAsync(command);

        if (cs != null)
        {
            cs.Cancel();
        }

    }
    #endregion


    #region FG Test
    public byte[] stringBuffer;
    public uint oldTime = 0;
    public uint Fg1Counter = 0;
    public uint fgMissingCount = 0;
    MeasurementData measurementData;
    public ConcurrentQueue<MeasurementData> measurementQueue;

    CancellationTokenSource cs;
    CancellationToken ct;

    public int sizeOfMeasurementStruct
    {
        get
        {
            return Marshal.SizeOf(typeof(MeasurementData));
        }
    }

    public void ParseHidReport(byte[] buffer)
    {
        // buffer[0]: data format
        //              - 0xA0  : fg data
        //              - 0XA1  : command
        //              - 0XA2  : flash data
        // buffer[1]: data count
        // buffer[2]: checksum
        int dataOffset = 3;
        byte command = buffer[0];
        int byteCount = buffer[1];  // byte count

        if (command == 0xA0) // fgdata
        {
            //dataOffset = 3;
            //for (int i = 0; i < byteCount; i++)
            //{
            //    fgQueue.Enqueue(buffer[i + dataOffset]);
            //}
        }
        else if (command == 0xA1) // command
        {
            dataOffset = 3;
            stringBuffer = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                stringBuffer[i] = buffer[i + dataOffset];
            }
            string commandString = Encoding.UTF8.GetString(stringBuffer);
            HidInputString += commandString;
        }
        else if (command == 0xA2) // flash data
        {
            //dataOffset = 2;
            //for (int i = 0; i < byteCount; i++)
            //{
            //    flashQueue.Enqueue(buffer[i + dataOffset]);
            //}
        }
        else if (command == 0xA3) // uart
        {
            //dataOffset = 3;
            //UART.stringBuffer = new byte[byteCount];
            //for (int i = 0; i < byteCount; i++)
            //{
            //    UART.stringBuffer[i] = buffer[i + dataOffset];
            //}

            //uart_byte_count += byteCount;

            //// Enqueue UART data
            //UART.dataQueue.Enqueue(UART.stringBuffer);

            //// print to console
            //UART.OnUARTReceived(null, UART.stringBuffer);
        }
        else if (command == 0xB0) // I2C
        {
            dataOffset = 3;
            stringBuffer = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                stringBuffer[i] = buffer[i + dataOffset];
            }

            HidInputString += "\nI2C: ";
            if (IsHidInputText)
            {
                HidInputString += ReplaceNonPrintableChar(Encoding.ASCII.GetString(stringBuffer, 0, byteCount));
            }
            else
            {
                StringBuilder sb = new StringBuilder(byteCount * 2);

                foreach (byte b in stringBuffer)
                {
                    sb.AppendFormat("{0:x2} ", b);
                }

                HidInputString += sb.ToString();
            }
            HidInputString += Environment.NewLine;

            //dataOffset = 3;
            //if (byteCount > 61)
            //    return;
            //I2C.stringBuffer = new byte[byteCount];
            //for (int i = 0; i < byteCount; i++)
            //{
            //    try
            //    {
            //        I2C.stringBuffer[i] = buffer[i + dataOffset];
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"{DateTime.Now} {ex.ToString()}");
            //    }
            //}

            //i2c_byte_count += byteCount;

            //// Enqueue I2C data
            //I2C.dataQueue.Enqueue(I2C.stringBuffer);

            //// print to console
            //I2C.OnI2CReceived(null, I2C.stringBuffer);
        }
        else if (command == 0xAA)
        {
            if (measurementData == null)
            {
                return;
            }

            dataOffset = 3;
            int blocks = byteCount / sizeOfMeasurementStruct;


            //byte checksum = 0;
            //for (int i = 0; i <　byteCount; i++)
            //{
            //    checksum += buffer[i + dataOffset];
            //}

            //if (checksum != buffer[2])
            //{
            //    checksumErrorCounter++;
            //    if (logger!= null)
            //    {
            //        logger.Error($"checksum error count = {checksumErrorCounter}");
            //    }
            //    return;
            //}


            for (int i = 0; i < blocks; i++)
            {
                //measurementData.period = BitConverter.ToUInt32(buffer, i * sizeOfMeasurementStruct + dataOffset);
                //measurementData.width = BitConverter.ToUInt32(buffer, i * sizeOfMeasurementStruct + dataOffset + 4);
                //byte current_lsb = buffer[i * sizeOfMeasurementStruct + dataOffset + 10];
                //byte current_msb = buffer[i * sizeOfMeasurementStruct + dataOffset + 11];
                //buffer[i * sizeOfMeasurementStruct + dataOffset + 10] = current_msb;
                //buffer[i * sizeOfMeasurementStruct + dataOffset + 11] = current_lsb;
                //byte voltage_lsb = buffer[i * sizeOfMeasurementStruct + dataOffset + 8];
                //byte voltage_msb = buffer[i * sizeOfMeasurementStruct + dataOffset + 9];
                //buffer[i * sizeOfMeasurementStruct + dataOffset + 8] = voltage_msb;
                //buffer[i * sizeOfMeasurementStruct + dataOffset + 9] = voltage_lsb;
                //measurementData.busVoltage = BitConverter.ToUInt16(buffer, i * sizeOfMeasurementStruct + dataOffset + 8);
                //measurementData.shuntVoltage = BitConverter.ToUInt16(buffer, i * sizeOfMeasurementStruct + dataOffset + 10);
                measurementData.time_ms = BitConverter.ToUInt32(buffer, i * sizeOfMeasurementStruct + dataOffset + 12);

                //measurementQueue.Enqueue(measurementData);

                Fg1Counter++;

                uint timedDiff = measurementData.time_ms - oldTime;
                if (timedDiff > 1)
                {
                    fgMissingCount = measurementData.time_ms - Fg1Counter;
                    string message = $"{DateTime.Now}======> Missing FG: {Fg1Counter}, {oldTime}, {measurementData.time_ms},{timedDiff}\n";
                    //WriteConsole(message);
                    Console.Write(message);
                }

                oldTime = measurementData.time_ms;

                if ((Fg1Counter % 1000) == 0)
                {
                    uint diff = measurementData.time_ms - Fg1Counter;
                    string message = $"{DateTime.Now} received = {Fg1Counter} send= {measurementData.time_ms} diff =  {diff} " +
                        $"error rate = {diff * 100.0 / measurementData.time_ms}%\n";
                    WriteConsole(message);
                    //Console.Write(message);

                    MyHidDevice.WriteCommand("DEBUG:rc\n");
                }
            }
        }
        else
        {
            //await Task.Delay(0);
        }
    }

    #endregion


    [RelayCommand]
    public void Test(object param)
    {
        Console.WriteLine("");
    }

}

