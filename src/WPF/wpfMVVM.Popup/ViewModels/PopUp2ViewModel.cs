using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfMVVM.Popup.ViewModels
{
    public partial class PopUp2ViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _text  = "PopUp2 View";
    }
}
