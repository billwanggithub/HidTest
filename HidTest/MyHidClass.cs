using CommunityToolkit.Mvvm.ComponentModel;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using HidSharp.Utility;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary> 
/*  ===========
 *  How to Use:
 *  ===========
 *  2025.6.6
    MyHidClass myHidDevice;
    myHidDevice = new MyHidClass(TestSetting.UsbVid, TestSetting.UsbPid);
    myHidDevice.InputReportReceived = InputReportReceived;
    MyHidClass.localDeviceList.Changed += DeviceListChangedHandler;
    DeviceListChangedHandler(null, null); // Check USB

    // Parse the HID Input Report
    void InputReportReceived(byte[] bytes, int length)
    {
    }

    // Deal with the USB plug & unplug
    void DeviceListChangedHandler(object? sender, EventArgs e)
    {        
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
 */
/// </summary>
public partial class MyHidClass : ObservableObject
{
    SemaphoreSlim semaphoreSlim = null;

    public delegate void InputReportReceivedDelegate(byte[] bytes, int length);
    public InputReportReceivedDelegate InputReportReceived;

    [ObservableProperty]
    public bool isConnected = false;

    public uint venderId { get; set; }
    public uint productId { get; set; }


    public static DeviceList localDeviceList;
    public static HidDevice[] hidDeviceArray;

    public HidDevice hidDevice = null;
    ReportDescriptor reportDescriptor;
    byte[] inputReportBuffer;
    HidDeviceInputReceiver inputReceiver;
    public HidStream hidStream;

    public MyHidClass(uint vid, uint pid)
    {
        HidSharpDiagnostics.EnableTracing = true;
        HidSharpDiagnostics.PerformStrictChecks = true;

        venderId = vid;
        productId = pid;

        localDeviceList = DeviceList.Local;
    }

    /// <summary>
    /// Check if the device is connected
    /// </summary>
    /// <returns></returns>
    public HidDevice CheckHidDevice()
    {
        hidDeviceArray = localDeviceList.GetHidDevices().ToArray();
        foreach (HidDevice dev in hidDeviceArray)
        {
            if ((dev.VendorID == venderId) && (dev.ProductID == productId))
            {
                hidDevice = dev;
                return hidDevice;
            }
        }
        IsConnected = false;
        hidDevice = null;
        return hidDevice;
    }

    /// <summary>
    /// Initialize the HID stuff
    /// </summary>
    public void OpenDevice()
    {
        if (hidDevice.TryOpen(out hidStream))
        {
            //hidStream.ReadTimeout = Timeout.Infinite;
            //hidStream.WriteTimeout = Timeout.Infinite;
            reportDescriptor = hidDevice.GetReportDescriptor();
            inputReportBuffer = new byte[hidDevice.GetMaxInputReportLength()];
            inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
            inputReceiver.Received += (sender, e) =>
            {
                Report report;
                while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                {
                    byte[] buffer = new byte[inputReportBuffer.Length - 1];
                    Array.Copy(inputReportBuffer, 1, buffer, 0, buffer.Length); // copy th hid report excluding the report id
                    InputReportReceived(buffer, buffer.Length);
                }
            };
            inputReceiver.Start(hidStream);
            IsConnected = true;
        }
    }

    /// <summary>
    /// Write byte array to HID
    /// </summary>
    /// <param name="buffer"></param>
    public async void WriteBytes(byte[] buffer, int timeout = 1)
    {
        if (hidStream == null)
            return;

        byte[] hidOutputReportBuffer = new byte[65];
        int count = buffer.Length;
        if (count > 64)
        {
            count = 64;
        }
        Array.Copy(buffer, 0, hidOutputReportBuffer, 1, count);
        if (semaphoreSlim != null) await semaphoreSlim?.WaitAsync();
        hidStream.Write(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length);
        if (semaphoreSlim != null) semaphoreSlim?.Release();
        await Task.Delay(timeout);
    }

    /// <summary>
    /// Write byte array to HID
    /// </summary>
    /// <param name="buffer"></param>
    public async Task WriteBytesAsync(byte[] buffer, int timeout = 1)
    {
        if (hidStream == null)
            return;

        byte[] hidOutputReportBuffer = new byte[65];
        int count = buffer.Length;
        if (count > 64)
        {
            count = 64;
        }
        Array.Copy(buffer, 0, hidOutputReportBuffer, 1, count);
        if (semaphoreSlim != null) await semaphoreSlim?.WaitAsync();
        await hidStream.WriteAsync(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length).ConfigureAwait(false);
        if (semaphoreSlim != null) semaphoreSlim?.Release();
        await Task.Delay(timeout);
    }

    /// <summary>
    /// Write string to HID. String size can be greater than 64 characters.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public void WriteString(string command, int timeout = 1)
    {
        if (hidStream == null)
            return;

        byte[] command_bytes = Encoding.ASCII.GetBytes(command);
        WriteBytes(command_bytes, timeout);
    }

    /// <summary>
    /// Write string to HID. String size can be greater than 64 characters.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task WriteStringAsync(string command, int timeout = 1)
    {
        byte[] command_bytes = Encoding.ASCII.GetBytes(command);
        await WriteBytesAsync(command_bytes, timeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Write string with command header 0xA1 to HID. String size can be greater than 64 characters.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async void WriteCommand(string command, int timeout = 1)
    {
        if (hidStream == null)
            return;

        byte[] hidOutputReportBuffer = new byte[65];

        /* Convert command text to byte array */
        string command_string = command.Replace("\\n", "\n");
        byte[] command_bytes = Encoding.ASCII.GetBytes(command_string);

        /* calculate command chunks number 
           split long command into small ones 
         */
        int command_chunk_size = 61;
        int command_length = command_string.Length;
        int command_remain_length = command_length % command_chunk_size;
        int command_chunk_number = command_length / command_chunk_size;
        if (command_remain_length > 0)
            command_chunk_number++;
        Array.Resize(ref command_bytes, command_chunk_number * command_chunk_size);

        //+ 分次送出 3 bytes header + 61 bytes command 
        for (int j = 0; j < command_chunk_number; j++)
        {
            /* ignore report id */

            /* data format */
            hidOutputReportBuffer[0] = 0x00;
            hidOutputReportBuffer[1] = 0xA1;

            /* byte count */
            //byte[] len_32bits = BitConverter.GetBytes(command_string.Length);
            if (j < (command_chunk_number - 1))
                hidOutputReportBuffer[2] = 61;
            else
                hidOutputReportBuffer[2] = (byte)command_remain_length;

            /* packet increasement */
            hidOutputReportBuffer[3] = 0;

            /* copy command string */
            Array.Copy(command_bytes, j * command_chunk_size, hidOutputReportBuffer, 4, command_chunk_size);
            //Array.Copy(hidOutputReportBuffer, 0, outputReportBuffer, 1, hidOutputReportBuffer.Length);
            //hidStream.Write(outputReportBuffer, 0, outputReportBuffer.Length);
            if (semaphoreSlim != null) await semaphoreSlim?.WaitAsync();
            hidStream.Write(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length);
            if (semaphoreSlim != null) semaphoreSlim?.Release();
        }
        await Task.Delay(timeout);
    }


    /// <summary>
    /// Write string with command header 0xA1 to HID. String size can be greater than 64 characters.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public async Task WriteCommandAsync(string command, int timeout = 1)
    {
        if (hidStream == null)
            return;

        byte[] hidOutputReportBuffer = new byte[65];

        /* Convert command text to byte array */
        string command_string = command.Replace("\\n", "\n");
        byte[] command_bytes = Encoding.ASCII.GetBytes(command_string);

        /* calculate command chunks number 
           split long command into small ones 
         */
        int command_chunk_size = 61;
        int command_length = command_string.Length;
        int command_remain_length = command_length % command_chunk_size;
        int command_chunk_number = command_length / command_chunk_size;
        if (command_remain_length > 0)
            command_chunk_number++;
        Array.Resize(ref command_bytes, command_chunk_number * command_chunk_size);

        //+ 分次送出 3 bytes header + 61 bytes command 
        for (int j = 0; j < command_chunk_number; j++)
        {
            /* ignore report id */

            /* data format */
            hidOutputReportBuffer[0] = 0x00;
            hidOutputReportBuffer[1] = 0xA1;

            /* byte count */
            //byte[] len_32bits = BitConverter.GetBytes(command_string.Length);
            if (j < (command_chunk_number - 1))
                hidOutputReportBuffer[2] = 61;
            else
                hidOutputReportBuffer[2] = (byte)command_remain_length;

            /* packet increasement */
            hidOutputReportBuffer[3] = 0;

            /* copy command string */
            Array.Copy(command_bytes, j * command_chunk_size, hidOutputReportBuffer, 4, command_chunk_size);
            //Array.Copy(hidOutputReportBuffer, 0, outputReportBuffer, 1, hidOutputReportBuffer.Length);
            //hidStream.Write(outputReportBuffer, 0, outputReportBuffer.Length);
            if (semaphoreSlim != null) await semaphoreSlim.WaitAsync();
            await hidStream.WriteAsync(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length).ConfigureAwait(false); ;
            if (semaphoreSlim != null) semaphoreSlim.Release();
        }
        await Task.Delay(timeout);
    }

}