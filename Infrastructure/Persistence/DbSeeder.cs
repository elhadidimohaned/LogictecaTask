using ClosedXML.Excel;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class DbSeeder // Change to non-static
    {
        private readonly ILogger<DbSeeder> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Semaphore for controlling access

        public DbSeeder(ILogger<DbSeeder> logger) // Constructor to inject logger
        {
            _logger = logger;
        }

        public async Task SeedAsync(ApplicationDbContext context, IProductRepository productRepository, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting database seeding process...");

            // Apply migrations
            await context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations applied successfully.");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // File path to the Excel file
            string excelFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "Persistence", "ExcelData", "20210106_Cisco_NVP_CE_Pricelist_Final.xlsx");
            excelFilePath = Path.GetFullPath(excelFilePath);
            _logger.LogInformation($"Excel File Path: {excelFilePath}");

            using var package = new ExcelPackage(new FileInfo(excelFilePath));

            var worksheets = package.Workbook.Worksheets.Take(3); // 1st, 2nd, and 3rd sheets
            var batchSize = 50000;

            foreach (var worksheet in worksheets)
            {
                var rowCount = worksheet.Dimension.End.Row; // Get the last row
                _logger.LogInformation($"Processing worksheet: {worksheet.Name} with {rowCount} rows.");

                var tasks = new List<Task>();

                for (int row = 3; row <= rowCount; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var partSKU = worksheet.Cells[row, 5].Text;
                    if (string.IsNullOrEmpty(partSKU))
                    {
                        _logger.LogInformation($"Skipping row {row} due to empty PartSKU.");
                        continue; // Skip this row
                    }
                    // Use the semaphore to control access
                    var productTask = ProcessProductAsync(context, worksheet, row, partSKU, productRepository);
                    tasks.Add(productTask);

                    if (tasks.Count >= batchSize)
                    {
                        await Task.WhenAll(tasks);
                        await productRepository.SaveChangesAsync(); // Save changes after each batch
                        tasks.Clear();
                    }
                }

                // Await remaining tasks after the loop
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks);
                    await productRepository.SaveChangesAsync(); // Save changes for remaining tasks
                }
            }

            _logger.LogInformation("Database seeding process completed successfully.");
        }

        private async Task ProcessProductAsync(ApplicationDbContext context, ExcelWorksheet worksheet, int row, string partSKU, IProductRepository productRepository)
        {
            await _semaphore.WaitAsync(); // Wait for access
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
                    _logger.LogInformation($"Added new product with SKU: {partSKU}");
                }
                else
                {
                    // Update existing product
                    product.Band = worksheet.Cells[row, 2].Text;
                    product.CategoryCode = worksheet.Cells[row, 3].Text;
                    product.Manufacturer = worksheet.Cells[row, 4].Text;
                    product.ItemDescription = worksheet.Cells[row, 6].Text;
                    product.ListPrice = ParseDecimal(worksheet.Cells[row, 7].Text);
                    product.MinDiscount = ParseDecimal(worksheet.Cells[row, 8].Text);
                    product.DiscountPrice = ParseDecimal(worksheet.Cells[row, 9].Text);
                    await productRepository.UpdateAsync(product);
                    _logger.LogInformation($"Updated product with SKU: {partSKU}");
                }
            }
            finally
            {
                _semaphore.Release(); // Always release the semaphore
            }
        }

        private decimal ParseDecimal(string value)
        {
            // Remove any non-numeric characters except the decimal point and negative sign
            string cleanedValue = Regex.Replace(value, @"[^\d.-]", "");

            // Parse the cleaned string to decimal
            if (decimal.TryParse(cleanedValue, out var result))
            {
                return result;
            }

            // If parsing fails, you can either return 0, throw an exception, or handle it as needed
            throw new FormatException($"Unable to parse '{value}' as a decimal.");
        }
    }
}
