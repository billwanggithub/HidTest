using System.Windows;

namespace HidTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                this.DataContext = App.Current.mainViewModel;
            };
        }
    }
}
