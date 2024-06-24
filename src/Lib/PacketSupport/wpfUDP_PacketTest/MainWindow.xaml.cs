using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpfUDP_PacketTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ComeNimonicDataModelHandler modal;
        public MainWindow()
        {
            InitializeComponent();

            modal = new ComeNimonicDataModelHandler();
            this.cbFieldName.ItemsSource = modal.GetModelsNameString();
        }


        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            var selectedFieldName = this.cbFieldName.SelectedItem.ToString();
            var selectedFieldValue = rbOne.IsChecked  == true ? 1d : 0d;
            SetClassProperty(modal.Model!, selectedFieldName!, selectedFieldValue);
            await modal.SendComeUDPNimoic();
        }

        private void SetClassProperty(object obj, string fieldName, double value)
        {
            var property = obj.GetType().GetProperty(fieldName);

            if(property != null && property.CanWrite)
            {
                property.SetValue(obj, value, null);
            }
        }
    }
}