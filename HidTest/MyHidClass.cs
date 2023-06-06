using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using HidSharp.Utility;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MyHidClass
{
    public delegate void InputReportReceivedDelegate(byte[] bytes, int length);
    public InputReportReceivedDelegate InputReportReceived;

    public bool isConnected;

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
        isConnected = false;
        hidDevice = null;
        return hidDevice;
    }

    public void OpenDevice()
    {
        if (hidDevice.TryOpen(out hidStream))
        {
            hidStream.ReadTimeout = Timeout.Infinite;
            hidStream.WriteTimeout = Timeout.Infinite;
            reportDescriptor = hidDevice.GetReportDescriptor();
            inputReportBuffer = new byte[hidDevice.GetMaxInputReportLength()];
            inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
            inputReceiver.Received += (sender, e) =>
            {
                Report report;
                while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                {
                    // Parse the report if possible.
                    Console.WriteLine("Reveived HID report");
                    byte[] buffer = new byte[inputReportBuffer.Length - 1];
                    Array.Copy(inputReportBuffer, 1, buffer, 0, buffer.Length);
                    InputReportReceived(buffer, buffer.Length);
                }
            };
            inputReceiver.Start(hidStream);
            isConnected = true;
        }
    }

    public void WriteBytes(byte[] buffer)
    {
        byte[] hidOutputReportBuffer = new byte[65];
        int count = buffer.Length;
        if (count > 64)
        {
            count = 64;
        }
        Array.Copy(buffer, 0, hidOutputReportBuffer, 1, count);
        hidStream.Write(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length);
    }

    public async Task WriteBytesAsync(byte[] buffer, int timeout = 1)
    {
        byte[] hidOutputReportBuffer = new byte[65];
        int count = buffer.Length;
        if (count > 64)
        {
            count = 64;
        }
        Array.Copy(buffer, 0, hidOutputReportBuffer, 1, count);
        await hidStream.WriteAsync(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length).ConfigureAwait(false);
        await Task.Delay(timeout);
    }

    public void WriteString(string command)
    {
        byte[] command_bytes = Encoding.ASCII.GetBytes(command);
        WriteBytes(command_bytes);
    }

    public async Task WriteStringAsync(string command)
    {
        byte[] command_bytes = Encoding.ASCII.GetBytes(command);
        await WriteBytesAsync(command_bytes).ConfigureAwait(false);
    }

    public void WriteCommand(string command, int timeout = 1)
    {
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
            hidStream.Write(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length);
        }
    }

    public async Task WriteCommandAsync(string command, int timeout = 1)
    {
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
            await hidStream.WriteAsync(hidOutputReportBuffer, 0, hidOutputReportBuffer.Length).ConfigureAwait(false); ;
        }
        await Task.Delay(timeout);
    }
}