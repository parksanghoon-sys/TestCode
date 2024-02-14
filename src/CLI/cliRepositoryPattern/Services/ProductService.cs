using cliRepositoryPattern.Interfaces;
using cliRepositoryPattern.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliRepositoryPattern.Services
{
    internal class ProductService
    {
        private readonly IRepository<Product> _productRepository;
        public ProductService(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }
        public void AddProduct(Product product)
        {
            _productRepository.InsertAsync(product);
        }
        /// ...
    }
}
