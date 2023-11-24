using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wpfMVVM.Popup.Base;
using wpfMVVM.Popup.ViewModels.Messaging;

namespace wpfMVVM.Popup.ViewModels
{
    public partial class ShellViewModel : ViewModelBase<IViewAction>
    {
        [ObservableProperty]
        private object? _currentDataContext;

        public ShellViewModel(IViewAction view)       
            :base(view) 
        {
            WeakReferenceMessenger.Default.Register<ShowMessageBoxMessage>(this, ShowMessageBox);
            WeakReferenceMessenger.Default.Register<object,string>(this, "CloseMsg", CloseMessageBox);
            CurrentDataContext = Ioc.Default.GetService<MainViewModel>();
        }

        private void CloseMessageBox(object recipient, object message)
        {
            base.View.CloseMessageBox();
        }

        private void ShowMessageBox(object recipient, ShowMessageBoxMessage message)
        {
            if(message.Value.Callback == null)
            {
                base.View.ShowMessageBox(message.Value.MessageText,
                    message.Value.MessagePopUpIconType,
                    message.Value.YesText,
                    message.Value.YesBtnColor,
                    message.Value.ConfirmCallback);
            }
            else
            {
                base.View.ShowYesNoMessgaeBox(message.Value.MessageText,
                    message.Value.MessagePopUpIconType,
                    message.Value.YesText,
                    message.Value.NoText,
                    message.Value.YesBtnColor,
                    message.Value.NoBtnColor,
                    message.Value.Callback);
            }
        }
    }
}
