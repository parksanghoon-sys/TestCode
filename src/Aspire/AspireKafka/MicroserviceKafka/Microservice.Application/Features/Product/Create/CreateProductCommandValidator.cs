using FluentValidation;
using Microservice.Application.Repositories;

namespace Microservice.Application.Features.Product.Command.Create
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        private readonly IProductRepository _orderRepository;

        public CreateProductCommandValidator(IProductRepository orderRepository)
        {
            _orderRepository = orderRepository;

            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .NotNull()
                .MaximumLength(50).WithMessage("{PropertyName} must be fewer than 50 characters");
        }

    }
}
