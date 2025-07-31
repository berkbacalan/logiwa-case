using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using EcomMMS.Infrastructure.Services;
using EcomMMS.Application.Common;
using FluentAssertions;
using Xunit;
using System.Text.Json;
using System.Text;

namespace EcomMMS.Tests.Features.Cache
{
    public class CacheServiceTests
    {
        private readonly Mock<IDistributedCache> _mockDistributedCache;
        private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
        private readonly RedisCacheService _cacheService;

        public CacheServiceTests()
        {
            _mockDistributedCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<RedisCacheService>>();
            _cacheService = new RedisCacheService(_mockDistributedCache.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_WhenCacheHit_ShouldReturnCachedValue()
        {
            // Given
            var testData = new TestData { Name = "Test", Value = 123 };
            var jsonData = JsonSerializer.Serialize(testData);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonData);
            
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync(jsonBytes);

            // When
            var result = await _cacheService.GetAsync<TestData>("test-key");

            // Then
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test");
            result.Value.Should().Be(123);
            _mockDistributedCache.Verify(x => x.GetAsync("test-key", default), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WhenCacheMiss_ShouldReturnNull()
        {
            // Given
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync((byte[]?)null);

            // When
            var result = await _cacheService.GetAsync<object>("test-key");

            // Then
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_ShouldCallDistributedCache()
        {
            // Given
            var testData = new TestData { Name = "Test", Value = 123 };

            // When
            await _cacheService.SetAsync("test-key", testData);

            // Then
            _mockDistributedCache.Verify(x => x.SetAsync(
                "test-key", 
                It.IsAny<byte[]>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldCallDistributedCache()
        {
            // Given
            var key = "test-key";

            // When
            await _cacheService.RemoveAsync(key);

            // Then
            _mockDistributedCache.Verify(x => x.RemoveAsync(key, default), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
        {
            // Given
            var jsonBytes = Encoding.UTF8.GetBytes("some-value");
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync(jsonBytes);

            // When
            var result = await _cacheService.ExistsAsync("test-key");

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyDoesNotExist_ShouldReturnFalse()
        {
            // Given
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync((byte[]?)null);

            // When
            var result = await _cacheService.ExistsAsync("test-key");

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheHit_ShouldReturnCachedValue()
        {
            // Given
            var testData = new TestData { Name = "Test", Value = 123 };
            var jsonData = JsonSerializer.Serialize(testData);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonData);
            
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync(jsonBytes);

            // When
            var result = await _cacheService.GetOrSetAsync("test-key", () => Task.FromResult(testData));

            // Then
            result.Should().BeEquivalentTo(testData);
            _mockDistributedCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
        }

        private class TestData
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        [Fact]
        public async Task GetOrSetAsync_WhenCacheMiss_ShouldCallFactoryAndCacheResult()
        {
            // Given
            var testData = new TestData { Name = "Test", Value = 123 };
            _mockDistributedCache.Setup(x => x.GetAsync("test-key", default))
                .ReturnsAsync((byte[]?)null);

            // When
            var result = await _cacheService.GetOrSetAsync("test-key", () => Task.FromResult(testData));

            // Then
            result.Should().BeEquivalentTo(testData);
            _mockDistributedCache.Verify(x => x.SetAsync(
                "test-key", 
                It.IsAny<byte[]>(), 
                It.IsAny<DistributedCacheEntryOptions>(), 
                default), Times.Once);
        }
    }
} 