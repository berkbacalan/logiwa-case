using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Infrastructure.Repositories;
using EcomMMS.Application.Common;
using EcomMMS.Infrastructure.Services;

namespace EcomMMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ICacheKeyGenerator, CacheKeyGenerator>();
            
            return services;
        }
    }
} 