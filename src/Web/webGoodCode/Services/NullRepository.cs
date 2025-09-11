using Microsoft.Data.Sqlite;
using webGoodCode.Models;

namespace webGoodCode.Services;

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

public class SqlReservationRepository : IReservationsRepository
{
    private readonly string _connectionString;

    private const string createReservationSql = @"
    INSERT INTO Reservations (
        Id, RestaurantId, At, Name, Email, Quantity
    ) VALUES (
        @Id, @RestaurantId, @At, @Name, @Email, @Quantity
    );";
    public SqlReservationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    public async Task Create(int restaurantId, Reservation reservation)
    {
        if (reservation is null)
            throw new ArgumentNullException(nameof(reservation));

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = createReservationSql;

        cmd.Parameters.AddWithValue("@Id", reservation.Id);
        cmd.Parameters.AddWithValue("@RestaurantId", restaurantId);
        cmd.Parameters.AddWithValue("@At", reservation.At);
        cmd.Parameters.AddWithValue("@Name", reservation.Name);
        cmd.Parameters.AddWithValue("@Email", reservation.Email);
        cmd.Parameters.AddWithValue("@Quantity", reservation.Quantity);

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
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
