using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using wpfMVVM.Popup.Enums;
using wpfMVVM.Popup.ViewModels.Messaging;

namespace wpfMVVM.Popup.Base
{
    public interface IViewAction
    {
        public void ShowMessageBox(string messgaeText, EMessagePopUpIconType messagePopUpIconType = EMessagePopUpIconType.None, string confirmText = "확인",
            Brush? confirmBtnColor = null, Action? callback = null);
        public void ShowYesNoMessgaeBox(string messgeText, EMessagePopUpIconType messagePopUpIconType = EMessagePopUpIconType.None,
            string yesText = "예", string noText = "아니요", Brush? yesBtnColor = null, Brush? noBtnColor = null,
            Action<bool?>? callback = null);

        public void CloseMessageBox();
    }
    public abstract class ViewModelBase<TViewAction> : ObservableObject
        where TViewAction : IViewAction
    {
        public ViewModelBase(TViewAction view)
        {
            View = view;
        }
        public TViewAction View { get; set; }
    }
}
