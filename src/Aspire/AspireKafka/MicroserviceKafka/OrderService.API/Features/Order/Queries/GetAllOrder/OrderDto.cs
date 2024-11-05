using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.API.Features.Order.Queries.GetAllOrder
{
    public class OrderDto
    {
        public string CustomerName { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }        
    }

}
