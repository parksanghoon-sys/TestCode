using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfMVVM.Popup.Base
{
    public partial class PopupDialogViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject? _popupVM;

        
        private RelayCommand? _closeCommand;

        public RelayCommand? CloseCommand
        {
            get
            {
                return _closeCommand ?? (_closeCommand = new RelayCommand(() => PopupVM = null));
            }
        }
        public virtual void Cleanup()
        {
            WeakReferenceMessenger.Default.Cleanup();
        }
    }
}
