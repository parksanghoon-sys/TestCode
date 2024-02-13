using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfMVVM.Popup.ViewModels
{
    public partial class PopUp1ViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _text = "PopUp1123123123 View";
    }
}
