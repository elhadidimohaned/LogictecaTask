using Application.Dtos;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IProductService
    {
        Task ImportProductsAsync(Stream excelStream);
        Task<List<ProductDto>> GetPaginatedProductsAsync(int offset, int pageSize, string? search, string? sortColumn, bool? sortDirection);
        Task<byte[]> ExportProductsAsync(string? search);
        IQueryable<Product> GetAllProducts();
    }
}

