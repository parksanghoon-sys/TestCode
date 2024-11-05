using AutoMapper;
using MediatR;
using Microservice.Application.Repositories;
using Microservice.Doamin;

namespace Microservice.Application.Features.Product.Command.Create
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
    {
        private readonly IMapper _mapper;
        private readonly IProductRepository _productRepository;

        public CreateProductCommandHandler(IMapper mapper, IProductRepository productRepository)
        {
            _mapper = mapper;
            _productRepository = productRepository;
        }
        public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var validator = new CreateProductCommandValidator(_productRepository);

            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if ((validationResult.Errors.Any()))
            {
                throw new Exception("Create Order Err");
            }

            var orderCreate = _mapper.Map<ProductModel>(request);
            await _productRepository.CreateAsync(orderCreate);

            return orderCreate.Id;
        }
    }
}
