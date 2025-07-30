using System;

namespace EcomMMS.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public int MinimumStockQuantity { get; private set; }

        private Category() { }

        public Category(string name, int minimumStockQuantity) : base()
        {
            ValidateName(name);
            ValidateMinimumStockQuantity(minimumStockQuantity);

            Name = name;
            MinimumStockQuantity = minimumStockQuantity;
        }

        public void UpdateName(string name)
        {
            ValidateName(name);
            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateMinimumStockQuantity(int minimumStockQuantity)
        {
            ValidateMinimumStockQuantity(minimumStockQuantity);
            MinimumStockQuantity = minimumStockQuantity;
            UpdatedAt = DateTime.UtcNow;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name cannot be empty.", nameof(name));

            if (name.Length > 100)
                throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));
        }

        private static void ValidateMinimumStockQuantity(int minimumStockQuantity)
        {
            if (minimumStockQuantity < 0)
                throw new ArgumentException("Minimum stock quantity cannot be negative.", nameof(minimumStockQuantity));
        }
    }
} 