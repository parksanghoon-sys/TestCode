﻿using webGoodCode.Models;

namespace webGoodCode.Services;

public interface IReservationsRepository
{
    Task Create(int restaurantId, Reservation reservation);

    Task<IReadOnlyCollection<Reservation>> ReadReservations(
        int restaurantId, DateTime min, DateTime max);

    Task<Reservation?> ReadReservation(int restaurantId, Guid id);

    Task Update(int restaurantId, Reservation reservation);

    Task Delete(int restaurantId, Guid id);
}
