using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesChartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalesChartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("sales-chart")]
        [SwaggerOperation(Summary = "Get sales chart data", Description = "Retrieve sales data grouped by date to generate a sales chart.")]
        [SwaggerResponse(200, "Returns the sales chart data", typeof(IEnumerable<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetSalesChart()
        {
            var sales = await _context.Sales.ToListAsync();
            var salesData = sales.GroupBy(s => s.Date.Date)
                                 .Select(g => new { Date = g.Key, TotalAmount = g.Sum(s => s.Amount) })
                                 .OrderBy(g => g.Date)
                                 .ToList();

            return Ok(salesData);
        }
    }
}
