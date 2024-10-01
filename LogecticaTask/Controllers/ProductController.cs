using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using LogecticaTask.Models;

namespace WebUI.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            await _productService.ImportProductsAsync(stream);

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var products = await _productService.GetPaginatedProductsAsync(page, pageSize, null, null, null);
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> ExportProducts([FromForm] DataTableRequest request)
        {
            var fileContent = await _productService.ExportProductsAsync(request.Search?.Value);
            var fileName = "ExportedProducts.xlsx";

            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


        [HttpPost]
        public async Task<IActionResult> GetProducts([FromForm] DataTableRequest request)
        {
            string? search = request.Search?.Value;
            // Apply ordering
            var sortColumn = request.Columns[request.Order[0].Column].Data;
            sortColumn = char.ToUpper(sortColumn[0]) + sortColumn.Substring(1);

            var sortDirection = request.Order[0].Dir.ToLower() == "asc";
            var products =await _productService.GetPaginatedProductsAsync(request.Start, request.Length, search, sortColumn, sortDirection);

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = products.Count,
                recordsFiltered = products.Count, // Update based on filtered count if applicable
                data = products
            });
        }


    }
}

