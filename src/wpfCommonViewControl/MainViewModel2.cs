using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfCommonViewControl
{
    internal partial class MainViewModel2 : ObservableObject
    {
        [ObservableProperty]
        private string _text;
        public MainViewModel2()
        {
            Text = "VIewModel2";
        }
    }
}
