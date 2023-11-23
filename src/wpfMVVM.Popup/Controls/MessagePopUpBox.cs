using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wpfMVVM.Popup.Enums;

namespace wpfMVVM.Popup.Controls
{
    public class MessagePopUpBox : UserControl
    {
        public event RoutedEventHandler? OKClick;
        public event RoutedEventHandler? NoClick;
        public event RoutedEventHandler? YesClick;

        public MessagePopUpBox()
        {
            DefaultStyleKey = typeof(MessagePopUpBox);
        }
        public static readonly DependencyProperty messagePopUpBoxTypeProperty =
            DependencyProperty.Register("messagePopUpBoxType", typeof(MessagePopUpBox), typeof(MessagePopUpBox));
        public EMessagePopUpBoxType messagePopUpBoxType
        {
            get { return (EMessagePopUpBoxType)GetValue(messagePopUpBoxTypeProperty); }
            set { SetValue(messagePopUpBoxTypeProperty, value); }
        }

        public static readonly DependencyProperty MessagePopUpIconTypeProperty =
            DependencyProperty.Register("MessagePopUpIconType", typeof(EMessagePopUpIconType), typeof(MessagePopUpBox));
        public EMessagePopUpIconType MessagePopUpIconType
        {
            get { return (EMessagePopUpIconType)this.GetValue(MessagePopUpIconTypeProperty); }
            set { this.SetValue(MessagePopUpIconTypeProperty, value); }
        }
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(MessagePopUpBox));
        public bool IsOpen
        {
            get { return (bool)this.GetValue(IsOpenProperty); }
            set { this.SetValue(IsOpenProperty, value); }
        }

        public static readonly DependencyProperty IsBackgroundDisableProperty =
            DependencyProperty.Register("IsBackgroundDisable", typeof(bool), typeof(MessagePopUpBox), new PropertyMetadata(false));
        public bool IsBackgroundDisable
        {
            get { return (bool)this.GetValue(IsBackgroundDisableProperty); }
            set { this.SetValue(IsBackgroundDisableProperty, value); }
        }

        public UIElement MsgBoxPlacementTarget
        {
            get { return (UIElement)GetValue(MsgBoxPlacementTargetProperty); }
            set { SetValue(MsgBoxPlacementTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MsgBoxPlacementTarget.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MsgBoxPlacementTargetProperty =
            DependencyProperty.Register("MsgBoxPlacementTarget", typeof(UIElement), typeof(MessagePopUpBox), new PropertyMetadata(null, OnmsgPlacementTargetPropertyChanged));



        private static void OnmsgPlacementTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
