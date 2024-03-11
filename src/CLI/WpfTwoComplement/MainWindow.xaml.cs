using cliompensatioinOfNegative2;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfTwoComplement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnCal_Click(object sender, RoutedEventArgs e)
        {
            BitCalculation bitCalculation = new BitCalculation();
            var targetText = this.txtTarget.Text;
            var bitLengthText = this.txtIndex.Text;

            var targetTextToInt = Convert.ToInt32(targetText);
            var bitLengthTxtToInt = Convert.ToInt32(bitLengthText);

            var result = bitCalculation.GetOriginalFromTwoComplement(targetTextToInt, bitLengthTxtToInt);
            this.tbResult.Text = result.ToString();
        }
    }
}
