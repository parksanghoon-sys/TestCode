
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpf.Ui.DataGrid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Test> vvv = new ObservableCollection<Test>();
        public MainWindow()
        {
            InitializeComponent();
            vvv.Add(new Test() { Value = "T1" });
            vvv.Add(new Test() { Value = "T2" });
            vvv.Add(new Test() { Value = "T3" });
            vvv.Add(new Test() { Value = "T4" });
            datagrid.DataContext = vvv;
        }
    }
    public class Test
    {
        public string? Value { get; set; }
    }
}
