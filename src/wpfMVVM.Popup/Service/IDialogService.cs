using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using wpfMVVM.Popup.Base;
using wpfMVVM.Popup.Enums;

namespace wpfMVVM.Popup.Service
{
    public interface IDialogService
    {
        void Register(EDialogHostType dialogHostType, Type dialogWindowHostType);
        bool CheckActivate(string title);
        void SetViewModel(ObservableObject vm, string? title, double width, double height, EDialogHostType dialogHostType, bool isModeal = true);
        void Clear();
    }
    public class DialogService : IDialogService
    {
        private Dictionary<EDialogHostType, Type> _dialogHostTypes;
        public DialogService()
        {
            _dialogHostTypes = new();
        }

        public void Register(EDialogHostType dialogHostType, Type dialogWindowHostType)
        {
            _dialogHostTypes.Add(dialogHostType, dialogWindowHostType);
        }
        public bool CheckActivate(string title)
        {
            var popupWIn = Application.Current.Windows.Cast<Window>().FirstOrDefault( p => p.Title == title);
            if(popupWIn != null)
            {
                popupWIn.Activate();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach(var window in Application.Current.Windows)
            {
                if(window is IDialog popupDialog)
                {
                    popupDialog.CloseCallback = null;
                    if(popupDialog.DataContext is PopupDialogViewModelBase vm)
                    {
                        vm.Cleanup();
                    }
                    popupDialog.DataContext = null;
                }
            }
            _dialogHostTypes.Clear();
        }


        public void SetViewModel(ObservableObject vm, string? title, double width, double height, EDialogHostType dialogHostType, bool isModeal = true)
        {
            Type dialogWindowHostType = _dialogHostTypes[dialogHostType];
            var popupDialog = Activator.CreateInstance(dialogWindowHostType) as IDialog;

            if (popupDialog == null)
                throw new Exception("팝업 다이얼로그를 생성할수 없다 IDialog 타입인지 체크");
            popupDialog.CloseCallback = () =>
            {
                popupDialog.CloseCallback = null;
                if(popupDialog.DataContext is PopupDialogViewModelBase vm)
                {
                    vm.Cleanup();
                }
                popupDialog.DataContext = null;
            };
            if(popupDialog.DataContext is PopupDialogViewModelBase viewModelBase)
            {
                popupDialog.Width = width;
                popupDialog.Height = height;
                popupDialog.Title = title;
                viewModelBase.PopupVM = vm;
                if(isModeal)
                {
                    popupDialog.ShowDialog();
                }
                else
                {
                    popupDialog.Show();
                }
            }

        }
    }
}
