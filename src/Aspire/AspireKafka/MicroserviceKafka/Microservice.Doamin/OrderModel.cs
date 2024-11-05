using Microservice.Doamin.Common;

namespace Microservice.Doamin
{
    public class OrderModel : BaseEntity
    {
        public string CustomerName { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
