using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

public partial class TestSettingClass : ObservableObject
{
    [JsonIgnore]
    [ObservableProperty]
    public int usbVid = 0x0483;
    [JsonIgnore]
    [ObservableProperty]
    public int usbPid = 0x5750;
}
