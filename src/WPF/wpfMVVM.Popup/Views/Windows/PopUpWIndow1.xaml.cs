using System;
using System.Collections.Generic;
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
    /// PopUpWIndow1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PopUpWIndow1 : Window, IDialog
    {
        public PopUpWIndow1()
        {
            this.DataContext = new PopupViewModel();
            InitializeComponent();
        }
        public Action? CloseCallback { get; set; }
        private void xTitleGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void xCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CloseCallback is not null)
                CloseCallback();

            this.xPopupContent.Content = null;
            this.Close();
        }
    }
}
