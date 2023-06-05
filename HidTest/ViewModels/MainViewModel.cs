using CommunityToolkit.Mvvm.ComponentModel;
using HidTest;
using System.Collections.Generic;
using System.Windows.Media;

namespace ViewModes
{
    public partial class MainViewModel : ObservableObject
    {
        ConsoleControl.WPF.ConsoleControl consoleControl;
        [ObservableProperty]
        public TestSettingClass testSetting = new();
        [ObservableProperty]
        public List<string> hidDeviceList = new() { "0483:5750" };
        [ObservableProperty]
        public int hidDeviceSelectedIndex = 0;

        public MainViewModel()
        {
            consoleControl = App.mainwindow.consoleCOntrol;
            WriteConsole("Test HID\n");
        }

        public void WriteConsole(string message, System.Windows.Media.Color? color = null)
        {
            consoleControl.WriteOutput(message, color ?? Colors.White);
        }
    }
}

