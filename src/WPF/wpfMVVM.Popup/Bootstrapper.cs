using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using wpfMVVM.Popup.Service;
using wpfMVVM.Popup.ViewModels;

namespace wpfMVVM.Popup
{
    internal class Bootstrapper
    {
        public Bootstrapper()
        {
            var service = ConfigureService();
            Ioc.Default.ConfigureServices(service);
        }

        private IServiceProvider ConfigureService()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDialogService,DialogService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<PopUp1ViewModel>();
            services.AddTransient<PopUp2ViewModel>();

            return services.BuildServiceProvider();
        }
    }
}