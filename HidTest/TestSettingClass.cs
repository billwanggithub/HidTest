using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

public partial class TestSettingClass : ObservableObject
{
    [JsonIgnore]
    [ObservableProperty]
    public uint usbVid;
    [JsonIgnore]
    [ObservableProperty]
    public uint usbPid;
}
