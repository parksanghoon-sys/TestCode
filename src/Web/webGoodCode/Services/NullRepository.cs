using webGoodCode.Models;

namespace webGoodCode.Services
{
    public class NullRepository : IReservationsRepository
    {
        public Task Create(int restaurantId, Reservation reservation)
        {
            return Task.CompletedTask;
        }

        public Task Delete(int restaurantId, Guid id)
        {
            return Task.CompletedTask;
        }

        public Task<Reservation?> ReadReservation(int restaurantId, Guid id)
        {
            return Task.FromResult(default(Reservation));
        }

        public Task<IReadOnlyCollection<Reservation>> ReadReservations(int restaurantId, DateTime min, DateTime max)
        {
            return Task.FromResult(default(IReadOnlyCollection<Reservation>));
        }

        public Task Update(int restaurantId, Reservation reservation)
        {
            return Task.CompletedTask;
        }
    }
}
