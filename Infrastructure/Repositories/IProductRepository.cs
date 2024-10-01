using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;

namespace Infrastructure.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetBySKUAsync(string partSKU);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task<List<Product>> GetPaginatedAsync(int offset, int pageSize, string? search, string? sortColumn, bool? sortDirection);
        Task UpdateRangeAsync(IEnumerable<Product> products);
        Task<List<Product>> GetAllAsync();
        IQueryable<Product> GetAll();
        Task SaveChangesAsync();
        Task AddRangeAsync(IEnumerable<Product> products);
    }
}

