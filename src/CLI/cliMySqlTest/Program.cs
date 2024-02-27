using System.Data.Entity;

class Program
{
    static void Main(string[] args)
    {
        IRepository<Person> repo = new Repository<Person>();

        // Insert
        Person person = new Person { Name = "HA", Email = "john@example.com" };
        repo.Insert(person);

        // Update
        person.Name = "Jane";
        repo.Update(person);

        // Get all
        var allPeople = repo.GetAll();

        // Get by id
        var singlePerson = repo.GetById(1);

        // Delete
        repo.Delete(1);
    }
}
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
public class MyDbContext : DbContext
{
    //public MyDbContext() : base("server=localhost;port=3306;database=test;uid=parksanghoon;password=tjb4048796") { }
    public MyDbContext() : base("server=localhost;database=test;uid=parksanghoon;password=tjb4048796") { }

    public DbSet<Person> People { get; set; }
}
public interface IRepository<T> where T : class
{
    IEnumerable<T> GetAll();
    T GetById(int id);
    void Insert(T obj);
    void Update(T obj);
    void Delete(int id);
}

public class Repository<T> : IRepository<T> where T : class
{
    private MyDbContext context = null;
    private DbSet<T> table = null;

    public Repository()
    {
        this.context = new MyDbContext();
        table = context.Set<T>();
    }

    public IEnumerable<T> GetAll()
    {
        return table.ToList();
    }

    public T GetById(int id)
    {
        return table.Find(id);
    }

    public void Insert(T obj)
    {
        table.Add(obj);
        context.SaveChangesAsync();
    }

    public void Update(T obj)
    {
        table.Attach(obj);
        context.Entry(obj).State = EntityState.Modified;
        context.SaveChanges();
    }

    public void Delete(int id)
    {
        T existing = table.Find(id);
        table.Remove(existing);
        context.SaveChanges();
    }
}
