using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using wpfMVVM.Popup.Service;
using wpfMVVM.Popup.ViewModels.Messaging;

namespace wpfMVVM.Popup.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }
        [RelayCommand]
        private void ShowConfirmMsg()
        {
            WeakReferenceMessenger.Default.Send(
            new ShowMessageBoxMessage(new MessageBoxInfo()
            {
                MessageText = "메세지창 내용",
                MessagePopUpIconType = Enums.EMessagePopUpIconType.None,
                YesText = "확인",
                YesBtnColor = null,
                ConfirmCallback = () =>
                {
                    MessageBox.Show("1");
                }
            }));
        }
        [RelayCommand]
        private void ShowConfirmMsg2()
        {
            WeakReferenceMessenger.Default.Send(new ShowMessageBoxMessage(
                new MessageBoxInfo()
            {
                MessageText = "메세지창 내용2",
                MessagePopUpIconType = Enums.EMessagePopUpIconType.Warning,
                YesText = "확인2",
                YesBtnColor = null,
                ConfirmCallback = () =>
                {
                    MessageBox.Show("2");
                }
            }));
        }

        [RelayCommand]
        private void ShowYesNoMsgExecute()
        {
            WeakReferenceMessenger.Default.Send(
                new ShowMessageBoxMessage(new MessageBoxInfo()
                {
                    MessageText = "메세지창 내용",
                    MessagePopUpIconType = Enums.EMessagePopUpIconType.None,
                    YesText = "좋다",
                    YesBtnColor = null,
                    NoText = "싫다",
                    Callback = (result) =>
                    {
                        MessageBox.Show(result.ToString());
                    }
                }));

            // 메세지 박스 닫기
            //WeakReferenceMessenger.Default.Send<object, string>("CloseMsg");
        }
        [RelayCommand]
        private void ShowPopup1()
        {
            if(_dialogService.CheckActivate("PopUp1") is true)
            {

            }else
            {
                var popup1vm = Ioc.Default.GetService<PopUp1ViewModel>();
                _dialogService.SetViewModel(popup1vm, "PopUp1", 500, 300, Enums.EDialogHostType.BasicType);
            }
        }
        [RelayCommand]
        private void ShowPopup2()
        {
            if (_dialogService.CheckActivate("PopUp2") is true)
            {

            }
            else
            {
                var popup2vm = Ioc.Default.GetService<PopUp2ViewModel>();
                _dialogService.SetViewModel(popup2vm, "PopUp2", 500, 300, Enums.EDialogHostType.AnotherType);
            }
        }
    }
}
