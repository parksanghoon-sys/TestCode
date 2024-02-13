using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using wpfMVVM.Popup.Base;

namespace wpfMVVM.Popup.Views.Windows
{
    /// <summary>
    /// PopUpWindow2.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PopUpWindow2 : Window, IDialog
    {
        public PopUpWindow2()
        {
            this.DataContext = new PopupViewModel();
            InitializeComponent();
            this.Closing += this.PopUpWindow2_Closing;
        }

        private void PopUpWindow2_Closing(object? sender, CancelEventArgs e)
        {
            if (CloseCallback is not null)
                CloseCallback();
        }

        public Action? CloseCallback {get; set;}
        
    }
}
