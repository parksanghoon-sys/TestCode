using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using wpfMVVM.Popup.Base;
using wpfMVVM.Popup.Enums;
using wpfMVVM.Popup.ViewModels;

namespace wpfMVVM.Popup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewAction
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ShellViewModel(this);
        }

        public void CloseMessageBox()
        {
            this.xMsgBox_Ok.IsOpen = false;
            this.xMsgBox_YesNo.IsOpen = false;
        }
        private bool _isConfirmMsgFirstEventHandler = false;
        private Action? _messageBoxConfirmResultCallback;

        public void ShowMessageBox(string messgaeText, EMessagePopUpIconType messagePopUpIconType = EMessagePopUpIconType.None, string confirmText = "확인", Brush? confirmBtnColor = null, Action? callback = null)
        {
            _messageBoxConfirmResultCallback = callback;

            if(_isConfirmMsgFirstEventHandler == false)
            {
                _isConfirmMsgFirstEventHandler= true;
                this.xMsgBox_Ok.OKClick += (s, e) =>
                {
                    if(_messageBoxConfirmResultCallback != null)
                    {
                        _messageBoxConfirmResultCallback();
                    }
                    _messageBoxConfirmResultCallback = null;
                };
            }
            this.xTxtMsgBox.Text = messgaeText;
            this.xMsgBox_Ok.ConfirmText = confirmText;
            if (confirmBtnColor == null)
                this.xMsgBox_Ok.YesBtnColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFD84C72")!);
            else
                this.xMsgBox_Ok.YesBtnColor = confirmBtnColor;
            this.xMsgBox_Ok.MessagePopUpIconType= messagePopUpIconType;
            this.xMsgBox_Ok.IsOpen = true;
        }

        private bool _isYesNoMsgFirstEventHandler = false;
        private Action<bool?>? _messageBoxResultCallback;

        public void ShowYesNoMessgaeBox(string messgeText, EMessagePopUpIconType messagePopUpIconType = EMessagePopUpIconType.None, string yesText = "예", string noText = "아니요", Brush? yesBtnColor = null, Brush? noBtnColor = null, Action<bool?>? callback = null)
        {
            _messageBoxResultCallback = callback;

            if (_isYesNoMsgFirstEventHandler == false)
            {
                _isYesNoMsgFirstEventHandler = true;
                this.xMsgBox_YesNo.YesClick += (s, e) =>
                {
                    if (_messageBoxResultCallback != null)
                        _messageBoxResultCallback(true);
                    _messageBoxResultCallback = null;
                };
                this.xMsgBox_YesNo.NoClick += (s, e) =>
                {
                    if (_messageBoxResultCallback != null)
                        _messageBoxResultCallback(false);
                    _messageBoxResultCallback = null;
                };
            }

            this.xTxtMsgBoxYesNo.Text = messgeText;
            this.xMsgBox_YesNo.YesText = yesText;
            this.xMsgBox_YesNo.NoText = noText;
            if (yesBtnColor == null)
            {
                this.xMsgBox_YesNo.YesBtnColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFD84C72")!);
            }
            else
            {
                this.xMsgBox_YesNo.YesBtnColor = yesBtnColor;
            }
            if (noBtnColor == null)
            {
                this.xMsgBox_YesNo.NoBtnColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF303A4D")!);
            }
            else
            {
                this.xMsgBox_YesNo.NoBtnColor = noBtnColor;
            }
            this.xMsgBox_YesNo.MessagePopUpIconType = messagePopUpIconType;
            this.xMsgBox_YesNo.IsOpen = true;
        }
    }
}
