using LibCQRS.Query;
using WebApplication1.Dtos;
using WebApplication1.Repository;

namespace WebApplication1.CQRS.Quries
{
    // Query definition
    public record GetUserByIdQuery(int UserId) : IQuery<UserDto?>;
    // Query handler
    public class GetUserByIdQueryHandler(IUserRepository userRepository) : IQueryHandler<GetUserByIdQuery, UserDto?>
    {
        public Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            var user = userRepository.GetById(query.UserId);
            return Task.FromResult(user?.ToDto());
        }
    }
}
