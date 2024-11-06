using Microservice.Doamin.Common;
using System.ComponentModel.DataAnnotations;

namespace Microservice.Doamin
{
    public class OrderModel : BaseEntity
    {
        [Required]
        public int  OrderId { get; set; }
        public string CustomerName { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
