using EcomMMS.Domain.Entities;
using System;
using System.Collections.Generic;

namespace EcomMMS.Tests.TestData
{
    public static class TestDataHelper
    {
        public static Category CreateTestCategory(string name = "Test Category", int minimumStockQuantity = 10)
        {
            return new Category(name, minimumStockQuantity);
        }

        public static Product CreateTestProduct(
            string title = "Test Product",
            string? description = "Test Description",
            Guid? categoryId = null,
            int stockQuantity = 15)
        {
            var category = CreateTestCategory();
            var product = new Product(title, description, category.Id, stockQuantity);
            product.SetCategory(category);
            return product;
        }

        public static List<Product> CreateTestProducts(int count = 5)
        {
            var products = new List<Product>();
            var category = CreateTestCategory();

            for (int i = 1; i <= count; i++)
            {
                var product = new Product(
                    $"Test Product {i}",
                    $"Test Description {i}",
                    category.Id,
                    i * 5);
                product.SetCategory(category);
                products.Add(product);
            }

            return products;
        }

        public static List<Category> CreateTestCategories(int count = 3)
        {
            var categories = new List<Category>();

            for (int i = 1; i <= count; i++)
            {
                categories.Add(new Category($"Test Category {i}", i * 5));
            }

            return categories;
        }
    }
} 