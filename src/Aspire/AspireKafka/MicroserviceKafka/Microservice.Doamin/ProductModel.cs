using Microservice.Doamin.Common;

namespace Microservice.Doamin
{
    public class ProductModel : BaseEntity
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
