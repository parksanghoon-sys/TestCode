using cliRepositoryPattern.Models;
using cliRepositoryPattern.Repositorys;
using cliRepositoryPattern.Services;
using Microsoft.EntityFrameworkCore;

public class Program
{
    static void Main(string[] args)
    {
        var dbContext = new DbContext();

        var userRepository = new UserRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);

        var userService = new UserService(userRepository);
        var productService = new ProductService(productRepository);

        var user = new User() { Id = 1, Name = "Jun" };
        var product = new Product() { Id = 1, Name = "Assistant" };

        userService.AddUser(user);
        productService.AddProduct(product);

        // 모든 User와 Product 정보를 가져와 출력
        var allUsers = userRepository.GetAllAsync();
        var allProducts = productRepository.GetAllAsync();

        Console.WriteLine("Users:");

        foreach (var u in allUsers.Result)
        {
            Console.WriteLine($"ID: {u.Id}, Name: {u.Name}");
        }

        Console.WriteLine("Products:");
        foreach (var p in allProducts.Result)
        {
            Console.WriteLine($"ID: {p.Id}, Name: {p.Name}");
        }
    }
}