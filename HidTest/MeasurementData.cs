using System.Runtime.InteropServices;

// https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute.pack?view=net-6.0
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public class MeasurementData
{
    public uint period = 1000000;
    public uint width = 0;
    public ushort busVoltage = 0;        // 195.3125 μV/LSB
    public ushort shuntVoltage = 0;      //312.5 nV/LSB
    public uint time_ms = 0;
}
