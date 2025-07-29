using System;

namespace EcomMMS.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public Guid CategoryId { get; private set; }
        public int StockQuantity { get; private set; }
        public bool IsLive { get; private set; }

        // Navigation property
        public Category? Category { get; private set; }

        private Product() { } // For EF Core

        public Product(string title, string? description, Guid categoryId, int stockQuantity)
        {
            ValidateTitle(title);
            ValidateStockQuantity(stockQuantity);
            ValidateCategoryId(categoryId);

            Title = title;
            Description = description;
            CategoryId = categoryId;
            StockQuantity = stockQuantity;
            CreatedAt = DateTime.UtcNow;
            
            UpdateIsLiveStatus();
        }

        public void UpdateTitle(string title)
        {
            ValidateTitle(title);
            Title = title;
            UpdatedAt = DateTime.UtcNow;
            UpdateIsLiveStatus();
        }

        public void UpdateDescription(string? description)
        {
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateCategory(Guid categoryId)
        {
            ValidateCategoryId(categoryId);
            CategoryId = categoryId;
            UpdatedAt = DateTime.UtcNow;
            UpdateIsLiveStatus();
        }

        public void UpdateStockQuantity(int stockQuantity)
        {
            ValidateStockQuantity(stockQuantity);
            StockQuantity = stockQuantity;
            UpdatedAt = DateTime.UtcNow;
            UpdateIsLiveStatus();
        }

        public void SetCategory(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            Category = category;
            CategoryId = category.Id;
            UpdateIsLiveStatus();
        }

        private void UpdateIsLiveStatus()
        {
            if (CategoryId == Guid.Empty)
            {
                IsLive = false;
                return;
            }

            if (Category != null && StockQuantity >= Category.MinimumStockQuantity)
            {
                IsLive = true;
            }
            else
            {
                IsLive = false;
            }
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Product title is required.", nameof(title));

            if (title.Length > 200)
                throw new ArgumentException("Product title cannot exceed 200 characters.", nameof(title));
        }

        private static void ValidateStockQuantity(int stockQuantity)
        {
            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));
        }

        private static void ValidateCategoryId(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
                throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        }
    }
} 