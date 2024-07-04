using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shop.Data;

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

        [HttpGet("sales-table")]
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
