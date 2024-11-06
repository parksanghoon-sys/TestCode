using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.API.Features.Order.Command.Create
{
    public class CreateOrderCommand : IRequest<int>
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string CustomerName { get; set; }
    }
}
