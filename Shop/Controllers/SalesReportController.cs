using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Shop.Data;
using Microsoft.AspNetCore.Authorization;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalesReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("text-report")]
        public async Task<IActionResult> GetTextReport()
        {
            var sales = await _context.Sales.ToListAsync();
            var groupedByDate = sales.GroupBy(s => s.Date.Date)
                                     .OrderByDescending(g => g.Count())
                                     .FirstOrDefault();
            var totalRevenue = sales.Sum(s => s.Revenue);
            var groupedByProduct = sales.GroupBy(s => s.ProductName)
                                        .Select(g => new { ProductName = g.Key, TotalRevenue = g.Sum(s => s.Revenue), TotalAmount = g.Sum(s => s.Amount) });

            var highestRevenueDay = sales.GroupBy(s => s.Date.Date)
                                         .Select(g => new { Date = g.Key, TotalRevenue = g.Sum(s => s.Revenue) })
                                         .OrderByDescending(g => g.TotalRevenue)
                                         .FirstOrDefault();

            var report = new StringBuilder();
            report.AppendLine("Sales Report:");
            report.AppendLine("==================");

            foreach (var sale in sales)
            {
                report.AppendLine($"Product: {sale.ProductName}, Amount: {sale.Amount}, Price: {sale.Price}, Revenue: {sale.Revenue}, Date: {sale.Date}");
            }

            if (groupedByDate != null)
            {
                report.AppendLine($"\nDate with highest sales: {groupedByDate.Key}, Sales Count: {groupedByDate.Count()}");
            }

            report.AppendLine($"\nTotal Revenue: {totalRevenue}");

            if (highestRevenueDay != null)
            {
                report.AppendLine($"\nDay with highest revenue: {highestRevenueDay.Date}, Revenue: {highestRevenueDay.TotalRevenue}");
            }

            report.AppendLine("\nProduct Statistics:");
            foreach (var product in groupedByProduct)
            {
                report.AppendLine($"Product: {product.ProductName}, Total Amount: {product.TotalAmount}, Total Revenue: {product.TotalRevenue}");
            }

            report.AppendLine("\nDaily Revenue:");
            foreach (var day in sales.GroupBy(s => s.Date.Date).Select(g => new { Date = g.Key, TotalRevenue = g.Sum(s => s.Revenue) }))
            {
                report.AppendLine($"Date: {day.Date}, Total Revenue: {day.TotalRevenue}");
            }

            return File(Encoding.UTF8.GetBytes(report.ToString()), "text/plain", "SalesReport.txt");
        }
    }
}
