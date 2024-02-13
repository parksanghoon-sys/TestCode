using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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

namespace wpfDatagridTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Person> _people;
        public ObservableCollection<Person> People
        {
            get { return _people; }
            set
            {
                _people = value;
                OnPropertyChagned(nameof(People));
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            People = new ObservableCollection<Person>
        {
            new Person { Frequncy = "", Age = 30, Name="Test1", Sex="남성" },
            new Person { Frequncy = "", Age = 25, Name="Test2", Sex="남성" },
            new Person { Frequncy = "", Age = 40, Name="Test3", Sex="남성" },
        };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChagned([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[A]?[0-9]*([.][0-9]{0,3})?$"); // 맨 앞에 'A'가 올 수 있고, 그 뒤에 소수점 3자리까지의 숫자만 허용. 소수점 다음에 숫자가 없는 경우도 허용.
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }
    }
    public class Person
    {
        public string? Frequncy { get; set; }
        public int Age { get; set; }
        public string Name{ get; set; }
        public string Sex { get; set; }
    }
}
