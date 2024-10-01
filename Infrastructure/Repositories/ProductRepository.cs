using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Product> products)
        {
            await _context.Products.AddRangeAsync(products);
        }
        public async Task UpdateRangeAsync(IEnumerable<Product> products)
        {
            _context.Products.UpdateRange(products); // Uses EF Core's UpdateRange method
            await Task.CompletedTask; // Placeholder to keep async signature
        }
        public async Task<Product?> GetBySKUAsync(string partSKU)
        {
            return await _context.Products.SingleOrDefaultAsync(p => p.PartSKU == partSKU);
        }


        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
        }

        public async Task<List<Product>> GetPaginatedAsync(int offset, int pageSize, string? search, string? sortColumn, bool? sortDirection)
        {
            var productsQuery = GetAll();

            // Apply search filter if there's any search term
            if (!string.IsNullOrEmpty(search))
            {
                var searchValue = search; // Keep the original case
                productsQuery = productsQuery.Where(p =>
                    p.Band.Contains(searchValue) ||
                    p.CategoryCode.Contains(searchValue) ||
                    p.Manufacturer.Contains(searchValue) ||
                    p.PartSKU.Contains(searchValue) ||
                    p.ItemDescription.Contains(searchValue) ||
                    p.ListPrice.ToString().Contains(searchValue) ||
                    p.MinDiscount.ToString().Contains(searchValue) ||
                    p.DiscountPrice.ToString().Contains(searchValue)
                );
            }

            // Apply ordering using OrderBy or OrderByDescending
            if(!string.IsNullOrEmpty(sortColumn) && sortDirection is not null)
            {
                productsQuery = (bool)sortDirection
                    ? productsQuery.OrderBy(x => EF.Property<object>(x, sortColumn))
                    : productsQuery.OrderByDescending(x => EF.Property<object>(x, sortColumn));
            }

            // Apply pagination
            var totalRecords = await productsQuery.CountAsync();


            var products = await productsQuery
                .Skip(offset * (pageSize-1))
                .Take(pageSize)
                .ToListAsync();
            return products;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public IQueryable<Product> GetAll()
        {
            return _context.Products.AsQueryable();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
