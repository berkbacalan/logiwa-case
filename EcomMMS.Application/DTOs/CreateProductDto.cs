namespace EcomMMS.Application.DTOs
{
    public class CreateProductDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public int StockQuantity { get; set; }
    }
} 