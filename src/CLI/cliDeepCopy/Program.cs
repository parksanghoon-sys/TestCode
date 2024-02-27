using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;

class Program
{
    static void Main(string[] args)
    {
        User user = new User();
        List<User> originalList = user.GetUserList(); // 원본 리스트
        List<User> copiedList = originalList.Select(user => new User(user)).ToList(); // 복사된 리스트
        List<User> copiedList2 = DeepCopy(originalList);

        foreach(var item in originalList)
        {
            item.NonSerializable.Id = 12;
            foreach(var node in item.NonSerializable.Test)
            {
                node.Replace('1', '9');
            }
            
        }
        foreach (var item in copiedList2)
        {
            Console.WriteLine(item.NonSerializable.Id);
            foreach (var node in item.NonSerializable.Test)
            {
                Console.WriteLine(node);
            }

        }
        foreach (var item in copiedList)
        {
            Console.WriteLine(item.NonSerializable.Id);
            foreach (var node in item.NonSerializable.Test)
            {
                Console.WriteLine(node);
            }

        }
    }
    public static T DeepCopy<T>(T obj)
    {
        string json = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
