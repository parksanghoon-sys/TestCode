using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfCommonViewControl
{
    internal partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _viewModel1;
        [ObservableProperty]
        private object _viewModel2;
        [ObservableProperty]
        private object _viewModel3;
        public MainViewModel()
        {
            ViewModel1 = new MainViewModel1();
            ViewModel2 = new MainViewModel2();
            ViewModel3 = new MainViewModel2();
        }
    }
}
