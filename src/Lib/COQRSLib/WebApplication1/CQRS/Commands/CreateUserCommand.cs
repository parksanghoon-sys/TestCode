using LibCQRS.Commands;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.CQRS.Commands
{
    // Command definition
    public record CreateUserCommand(string UserName, string Email) : ICommand<UserDto>;
    public class CreateUserCommandHandler(IUserRepository userRepository) : ICommandHandler<CreateUserCommand, UserDto>
    {
        public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            var user = new User(command.UserName, command.Email);

            // Create and return DTO from saved domain object
            var createdUser  = await userRepository.CreateUserAsync(user);

            return createdUser.ToDto();
        }
    }
}
