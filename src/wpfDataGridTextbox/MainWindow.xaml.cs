using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpfDataGridTextbox;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("^[A]?[0-9]*([.][0-9]{1,3})?$"); // 맨 앞에 'A'가 올 수 있고, 그 뒤에 소수점 3자리까지의 숫자만 허용
        e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
    }
}