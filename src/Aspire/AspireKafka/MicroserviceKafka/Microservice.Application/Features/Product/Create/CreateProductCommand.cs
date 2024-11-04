using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Application.Features.Product.Command.Create
{
    public class CreateProductCommand : IRequest<int>
    {
        public int Quantity { get; set; }
        public string Name { get; set; }
    }
}
