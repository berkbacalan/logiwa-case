using MediatR;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Application.DTOs;
using EcomMMS.Application.Common;
using System.Linq;

namespace EcomMMS.Application.Features.Products.Queries.GetFilteredProducts
{
    public class GetFilteredProductsQueryHandler : IRequestHandler<GetFilteredProductsQuery, Result<IEnumerable<ProductDto>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public GetFilteredProductsQueryHandler(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<IEnumerable<ProductDto>>> Handle(GetFilteredProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();

            var query = products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(p => p.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       (p.Description != null && p.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.MinStockQuantity.HasValue)
            {
                query = query.Where(p => p.StockQuantity >= request.MinStockQuantity.Value);
            }

            if (request.MaxStockQuantity.HasValue)
            {
                query = query.Where(p => p.StockQuantity <= request.MaxStockQuantity.Value);
            }

            if (request.IsLive.HasValue)
            {
                query = query.Where(p => p.IsLive == request.IsLive.Value);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }

            var filteredProducts = query.ToList();

            var productDtos = new List<ProductDto>();
            foreach (var product in filteredProducts)
            {
                var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
                
                productDtos.Add(new ProductDto
                {
                    Id = product.Id,
                    Title = product.Title,
                    Description = product.Description,
                    CategoryId = product.CategoryId,
                    CategoryName = category?.Name ?? string.Empty,
                    StockQuantity = product.StockQuantity,
                    IsLive = product.IsLive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                });
            }

            var skip = (request.Page - 1) * request.PageSize;
            var paginatedProducts = productDtos
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            return Result<IEnumerable<ProductDto>>.Success(paginatedProducts);
        }
    }
} 