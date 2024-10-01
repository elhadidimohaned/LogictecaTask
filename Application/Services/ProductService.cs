using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.Interfaces;
using ClosedXML.Excel;
using Domain.Entities;
using Infrastructure.Persistence;
using Application.Dtos;
using Infrastructure.Repositories;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }
        public IQueryable<Product> GetAllProducts()
        {
            return _repository.GetAll();
        }
        public async Task ImportProductsAsync(Stream excelStream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(excelStream);

            var worksheets = package.Workbook.Worksheets.Take(3);
            var batchSize = 100000;

            foreach (var worksheet in worksheets)
            {
                var rowCount = worksheet.Dimension.End.Row;
                var tasks = new List<Task>();

                for (int row = 3; row <= rowCount; row++)
                {

                    var partSKU = worksheet.Cells[row, 5].Text;
                    // Check if partSKU is null or empty
                    if (string.IsNullOrEmpty(partSKU))
                    {
                        continue; // Skip this row
                    }
                    // Use the semaphore to control access
                    var productTask = ProcessProductAsync(worksheet, row, partSKU, _repository);
                    tasks.Add(productTask);

                    if (tasks.Count >= batchSize)
                    {
                        await Task.WhenAll(tasks);
                        await _repository.SaveChangesAsync(); // Save changes after each batch
                        tasks.Clear();
                    }
                }

                // Await remaining tasks after the loop
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                    await _repository.SaveChangesAsync(); // Save changes for remaining tasks
                }
            }

        }

        private async Task ProcessProductAsync(ExcelWorksheet worksheet, int row, string partSKU, IProductRepository productRepository)
        {
            await _semaphore.WaitAsync();
            try
            {
                var product = await productRepository.GetBySKUAsync(partSKU);

                if (product == null)
                {
                    // Create new product
                    product = new Product
                    {
                        Band = worksheet.Cells[row, 2].Text,
                        CategoryCode = worksheet.Cells[row, 3].Text,
                        Manufacturer = worksheet.Cells[row, 4].Text,
                        PartSKU = partSKU,
                        ItemDescription = worksheet.Cells[row, 6].Text,
                        ListPrice = ParseDecimal(worksheet.Cells[row, 7].Text),
                        MinDiscount = ParseDecimal(worksheet.Cells[row, 8].Text),
                        DiscountPrice = ParseDecimal(worksheet.Cells[row, 9].Text)
                    };

                    await productRepository.AddAsync(product);
                }
                else
                {
                    product.Band = worksheet.Cells[row, 2].Text;
                    product.CategoryCode = worksheet.Cells[row, 3].Text;
                    product.Manufacturer = worksheet.Cells[row, 4].Text;
                    product.ItemDescription = worksheet.Cells[row, 6].Text;
                    product.ListPrice = ParseDecimal(worksheet.Cells[row, 7].Text);
                    product.MinDiscount = ParseDecimal(worksheet.Cells[row, 8].Text);
                    product.DiscountPrice = ParseDecimal(worksheet.Cells[row, 9].Text);
                    await productRepository.UpdateAsync(product);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private decimal ParseDecimal(string value)
        {
            string cleanedValue = Regex.Replace(value, @"[^\d.-]", "");

            if (decimal.TryParse(cleanedValue, out var result))
            {
                return result;
            }

            throw new FormatException($"Unable to parse '{value}' as a decimal.");
        }
        public async Task<List<ProductDto>> GetPaginatedProductsAsync(int offset, int pageSize, string? search, string? sortColumn, bool? sortDirection)
        {
            var products = await _repository.GetPaginatedAsync(offset, pageSize, search, sortColumn, sortDirection);
            return products.Select(p => new ProductDto
            {
                Band = p.Band,
                CategoryCode = p.CategoryCode,
                Manufacturer = p.Manufacturer,
                PartSKU = p.PartSKU,
                ItemDescription = p.ItemDescription,
                ListPrice = p.ListPrice,
                MinDiscount = p.MinDiscount,
                DiscountPrice = p.DiscountPrice
            }).ToList();
        }

        public async Task<byte[]> ExportProductsAsync(string? search)
        {
            var productsQuery = GetAllProducts(); // Fetch products as IQueryable

            // Apply search filter if there's any search term
            if (!string.IsNullOrEmpty(search))
            {
                var searchValue = search;
                productsQuery = productsQuery.Where(p =>
                    p.Band.ToLower().Contains(searchValue) ||
                    p.CategoryCode.ToLower().Contains(searchValue) ||
                    p.Manufacturer.ToLower().Contains(searchValue) ||
                    p.PartSKU.ToLower().Contains(searchValue) ||
                    p.ItemDescription.ToLower().Contains(searchValue) ||
                    p.ListPrice.ToString().Contains(searchValue) ||
                    p.MinDiscount.ToString().Contains(searchValue) ||
                    p.DiscountPrice.ToString().Contains(searchValue)
                );
            }

            var filteredProducts = await productsQuery.ToListAsync();
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Products");

            worksheet.Cell(1, 1).Value = "Band";
            worksheet.Cell(1, 2).Value = "CategoryCode";
            worksheet.Cell(1, 3).Value = "Manufacturer";
            worksheet.Cell(1, 4).Value = "PartSKU";
            worksheet.Cell(1, 5).Value = "ItemDescription";
            worksheet.Cell(1, 6).Value = "ListPrice";
            worksheet.Cell(1, 7).Value = "MinDiscount";
            worksheet.Cell(1, 8).Value = "DiscountPrice";

            var row = 2;
            foreach (var product in filteredProducts)
            {
                worksheet.Cell(row, 1).Value = product.Band;
                worksheet.Cell(row, 2).Value = product.CategoryCode;
                worksheet.Cell(row, 3).Value = product.Manufacturer;
                worksheet.Cell(row, 4).Value = product.PartSKU;
                worksheet.Cell(row, 5).Value = product.ItemDescription;
                worksheet.Cell(row, 6).Value = product.ListPrice;
                worksheet.Cell(row, 7).Value = product.MinDiscount;
                worksheet.Cell(row, 8).Value = product.DiscountPrice;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}

