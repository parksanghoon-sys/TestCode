using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfDataGridTextbox
{
    internal class MainViewModel : ViewModelBase
    {
        private ObservableCollection<Person> _people;
        public ObservableCollection<Person> People
        {
            get { return _people; }
            set
            {
                _people = value;
                OnPropertyChanged("People");
            }
        }

        public MainViewModel()
        {
            People = new ObservableCollection<Person>
        {
            new Person { Name = "123.000", Age = 30 },
            new Person { Name = "456.22", Age = 25 },
            new Person { Name = "A22.333", Age = 40 },
        };
        }
        
    }
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
