namespace EcomMMS.Application.DTOs
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public PaginationMetadata Metadata { get; set; } = new(1, 10, 0);
    }

    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public int? PreviousPage { get; set; }
        public int? NextPage { get; set; }

        public PaginationMetadata(int currentPage, int pageSize, int totalCount)
        {
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            HasPreviousPage = currentPage > 1;
            HasNextPage = currentPage < TotalPages;
            PreviousPage = HasPreviousPage ? currentPage - 1 : null;
            NextPage = HasNextPage ? currentPage + 1 : null;
        }
    }
} 