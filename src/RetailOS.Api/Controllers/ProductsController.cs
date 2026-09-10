using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Products.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductImportService _importService;

    public ProductsController(IProductService productService, IProductImportService importService)
    {
        _productService = productService;
        _importService = importService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProductListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? inStock = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _productService.GetProductsAsync(page, pageSize, search, categoryId, isActive, inStock, cancellationToken);
        return Ok(ApiResponse<ProductListResponse>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _productService.GetProductByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProductResponse>.Ok(response));
    }

    [HttpGet("barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductByBarcode([FromRoute] string barcode, CancellationToken cancellationToken)
    {
        var response = await _productService.GetProductByBarcodeAsync(barcode, cancellationToken);
        return Ok(ApiResponse<ProductResponse>.Ok(response));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.CreateProductAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProductResponse>.Ok(response, "Product created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ProductResponse>.Ok(response, "Product updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductStatus([FromRoute] Guid id, [FromBody] UpdateProductStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.UpdateProductStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ProductResponse>.Ok(response, "Product status updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("import/preview")]
    [Authorize(Roles = Roles.Owner)]
    [RequestSizeLimit(5_242_880)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ImportPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewImport(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("FILE_REQUIRED", "Please provide a valid file to import."));

        await using var stream = file.OpenReadStream();
        var response = await _importService.PreviewImportAsync(stream, file.FileName, cancellationToken);
        return Ok(ApiResponse<ImportPreviewResponse>.Ok(response));
    }

    [HttpPost("import/commit")]
    [Authorize(Roles = Roles.Owner)]
    [RequestSizeLimit(5_242_880)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ImportCommitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CommitImport(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("FILE_REQUIRED", "Please provide a valid file to import."));

        await using var stream = file.OpenReadStream();
        var response = await _importService.CommitImportAsync(stream, file.FileName, cancellationToken);
        return Ok(ApiResponse<ImportCommitResponse>.Ok(response, "Import processed successfully."));
    }
}
