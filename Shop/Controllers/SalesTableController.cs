using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesTableController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalesTableController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get sales table data
        /// </summary>
        /// <remarks>
        /// Retrieve sales data ordered by date to generate a sales table.
        /// </remarks>
        /// <returns>A list of sales data including product name, amount, and date.</returns>
        /// <response code="200">Returns the sales table data</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("sales-table")]
        [SwaggerOperation(Summary = "Get sales table data", Description = "Retrieve sales data ordered by date to generate a sales table.")]
        [SwaggerResponse(200, "Returns the sales table data", typeof(IEnumerable<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetSalesTable()
        {
            var sales = await _context.Sales.ToListAsync();
            var salesData = sales.OrderBy(s => s.Date)
                                 .Select(s => new { s.ProductName, s.Amount, s.Date })
                                 .ToList();

            return Ok(salesData);
        }
    }
}
