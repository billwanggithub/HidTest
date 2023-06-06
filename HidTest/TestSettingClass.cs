using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

public partial class TestSettingClass : ObservableObject
{
    [JsonIgnore]
    [ObservableProperty]
    public uint usbVid = 0x0483;
    [JsonIgnore]
    [ObservableProperty]
    public uint usbPid = 0x5750;
}
