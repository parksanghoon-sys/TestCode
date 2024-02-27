[Serializable]
public class User
{
    private List<User> users = new List<User>();
    public string Name { get; set; }
    public int Age { get; set; }
    public NonSerializableType NonSerializable { get; set; }
    public User()
    {
      
    }
    public List<User> GetUserList()
    {
        users = new List<User>()
        {
            new User(){Name = "Test1" , Age = 0 , NonSerializable = new NonSerializableType(1)},
            new User(){Name = "Test1" , Age = 0 , NonSerializable = new NonSerializableType(2)},
            new User(){Name = "Test1" , Age = 0 , NonSerializable = new NonSerializableType(3)},
        };
        return users;
    }
    // 복사 생성자
    public User(User other)
    {
        this.Name = other.Name;
        this.Age = other.Age;
        this.NonSerializable = other.NonSerializable;
    }
}