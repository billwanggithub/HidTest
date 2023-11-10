using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace HidTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider Services { get; }
        public new static App Current => (App)Application.Current;
        public static MainWindow mainwindow { get; set; } = new MainWindow();
        public MainViewModel mainViewModel; //用App.Current.mainViewModel呼叫

        public App()
        {
            Services = ConfigureServices();
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MainViewModel>(); // 整個 Process 只建立一個 Instance，任何時候都共用它, 要整個 Process 共用一份的服務可註冊成 Singleton
            return services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            mainViewModel = Services?.GetService<MainViewModel>(); // Set mainViewModel

            this.MainWindow = mainwindow;
            this.MainWindow.Top = 0;
            this.MainWindow.Left = 0;
            this.MainWindow.Show();


            base.OnStartup(e);
        }
    }
}
