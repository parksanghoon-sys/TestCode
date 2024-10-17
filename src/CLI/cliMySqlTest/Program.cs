using MySql.Data.MySqlClient;
using System.Data.Entity;

class Program
{
    static async Task Main(string[] args)
    {
        MysqlConnectionTest mysqlConnectionTest = new MysqlConnectionTest();
        //IRepository<Person> repo = new Repository<Person>();

        //// Insert
        //Person person = new Person { Name = "HA", Email = "john@example.com" };
        //await repo.Insert(person);

        //// Update
        //person.Name = "Jane";
        //await repo.Update(person);

        //// Get all
        //var allPeople = repo.GetAll();

        //// Get by id
        //var singlePerson = repo.GetById(1);

        //// Delete
        //await repo.Delete(1);
    }
}
public class MysqlConnectionTest
{
    public MysqlConnectionTest()
    {
        string server = "localhost";
        string database = "db_hr_leavemanagement";
        string user = "parksanghoon";
        string password = "tjb4048796"; // MySQL의 비밀번호를 여기에 입력

        string connectionString = $"Server={server};Database={database};User ID={user};Password={password};";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                Console.WriteLine("MySQL 데이터베이스에 성공적으로 접속했습니다.");

                string query = "SELECT VERSION()"; // MySQL 서버 버전을 가져오는 쿼리
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    string version = cmd.ExecuteScalar().ToString();
                    Console.WriteLine($"MySQL 버전: {version}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"데이터베이스 연결 실패: {ex.Message}");
        }
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
    Task Insert(T obj);
    Task Update(T obj);
    Task Delete(int id);
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

    public async Task Insert(T obj)
    {
        table.Add(obj);
        await context.SaveChangesAsync();
    }

    public async Task Update(T obj)
    {
        table.Attach(obj);
        context.Entry(obj).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        T existing = table.Find(id);
        table.Remove(existing);
        await context.SaveChangesAsync();
    }
}
