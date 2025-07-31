using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcomMMS.Domain.Interfaces;
using EcomMMS.Infrastructure.Repositories;
using EcomMMS.Application.Common;
using EcomMMS.Infrastructure.Services;
using StackExchange.Redis;

namespace EcomMMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            var redisConnectionString = configuration["Redis:ConnectionString"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(provider =>
                {
                    return ConnectionMultiplexer.Connect(redisConnectionString);
                });
            }
            
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ICacheKeyGenerator, CacheKeyGenerator>();
            
            return services;
        }
    }
} 