using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using wpfMVVM.Popup.Enums;
using wpfMVVM.Popup.Service;
using wpfMVVM.Popup.Views.Windows;

namespace wpfMVVM.Popup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            new Bootstrapper();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var dialogService = Ioc.Default.GetService<IDialogService>();
            dialogService!.Register(EDialogHostType.BasicType, typeof(PopUpWIndow1));
            dialogService!.Register(EDialogHostType.AnotherType, typeof(PopUpWindow2));

            var shellWIndow = new MainWindow();
            shellWIndow.ShowDialog();
            if(Current != null)
            {
                Current.Shutdown();
            }
        }
    }
}
