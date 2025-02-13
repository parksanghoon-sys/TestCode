using Publisher;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

internal class Program
{
    static async Task Main()
    {
        var manager = new SubscriptionManager();
        var person = new Person { Name = "Alice", Age = 25 };
        var employee = new Employee { JobTitle = "Developer" };

        var personSubscription = manager.CreateSubscription<Person>(person);
        var employeeSubscription = manager.CreateSubscription(employee);

        // Person에 대한 구독자 추가
        personSubscription.Subscribe<Person>(async (property, value) =>
        {
            await Task.Delay(100);
            Console.WriteLine($"[Person] Property Changed: {property}, New Value: {value}");
        });

        // Employee에 대한 구독자 추가
        employeeSubscription.Subscribe<Employee>(async (property, value) =>
        {
            await Task.Delay(100);
            Console.WriteLine($"[Employee] Property Changed: {property}, New Value: {value}");
        });

        // 속성 변경 (해당하는 클래스의 구독자에게만 알림 전송)
        person.Name = "Charlie";  // 출력 (100ms 후): [Person] Property Changed: Name, New Value: Charlie
        person.Age = 35;          // 출력 (100ms 후): [Person] Property Changed: Age, New Value: 35
        employee.JobTitle = "Senior Developer"; // 출력 (100ms 후): [Employee] Property Changed: JobTitle, New Value: Senior Developer

        // Employee 구독 해제 후 변경 (출력 없음)
        employeeSubscription.Unsubscribe<Employee>(async (property, value) =>
        {
            Console.WriteLine($"[Employee] Unsubscribed: {property}, Value: {value}");
        });

        employee.JobTitle = "Lead Developer"; // 출력 없음

        // 구독 서비스 제거
        manager.RemoveSubscription(person);
        manager.RemoveSubscription(employee);

        Console.ReadLine();

    }
}
public class Person : ObservableObject
{
    private string _name;
    private int _age;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public int Age
    {
        get => _age;
        set
        {
            if (_age != value)
            {
                _age = value;
                OnPropertyChanged();
            }
        }
    }
}

public class Employee : ObservableObject
{
    private string _jobTitle;

    public string JobTitle
    {
        get => _jobTitle;
        set
        {
            if (_jobTitle != value)
            {
                _jobTitle = value;
                OnPropertyChanged();
            }
        }
    }
}

public abstract class ObservableObject : INotifyPropertyChanged
{    
    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
