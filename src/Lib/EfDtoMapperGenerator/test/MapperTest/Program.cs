using MapperGenerator.Attributes;

namespace MapperTest;
internal class Program
{
    private static void Main(string[] args)
    {
        var entity = new UserEntity { Id = 1, Name = "홍길동", Email = "hong@test.com", CreatedAt = DateTime.Now };

        // Entity → DTO
        var dto = entity.ToUserDto();
        Console.WriteLine($"DTO: {dto.Id}, {dto.FileName}, {dto.Email}");

        // DTO → Entity
        var newEntity = dto.ToUserEntity();
        Console.WriteLine($"Entity: {newEntity.Id}, {newEntity.Name}, {newEntity.Email}");
    }
}
[MapTo(typeof(UserDto))]
public class UserEntity
{
    public int Id { get; set; }
    [MapProperty("FileName")]
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> Object { get; set; } = new();

    [MapIgnore] // 매핑 제외
    public DateTime CreatedAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public List<string> Object { get; set; } = new();
    public string Email { get; set; } = "";
}