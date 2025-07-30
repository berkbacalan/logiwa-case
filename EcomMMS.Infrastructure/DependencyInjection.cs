using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Infrastructure.Repositories;

namespace EcomMMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            
            return services;
        }
    }
} 