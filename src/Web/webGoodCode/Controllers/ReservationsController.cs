using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using webGoodCode.Models;
using webGoodCode.Restarant.RestApi;
using webGoodCode.Services;

namespace webGoodCode.Controllers
{
    [Route("[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationsRepository _reservationsRepository;

        public ReservationsController(IReservationsRepository reservationsRepository)
        {
            _reservationsRepository = reservationsRepository;
        }
#pragma warning disable CA1822
        public async Task Post(ReservationDto dto)
        {
            if(dto is null)
                throw new ArgumentNullException(nameof(dto));
            
            await _reservationsRepository.Create( 1,
                    new Reservation(dto.ParseId() ?? Guid.NewGuid(), dto.At, new Email(dto.Email), new Name(dto.Name), 5)
                );
        }
#pragma warning restore CA1822
    }
}
