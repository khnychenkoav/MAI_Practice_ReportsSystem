using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Shop.Data;
using Shop.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesExcelController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalesExcelController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get sales data as an Excel file
        /// </summary>
        /// <remarks>
        /// Retrieve sales data and statistics in an Excel file.
        /// Example XML:
        /// <Sale>
        /// <Id>1</Id>
        /// <ProductName>Product A</ProductName>
        /// <Amount>10</Amount>
        /// <Date>2023-07-01T00:00:00</Date>
        /// <Price>100</Price>
        /// <Username>User1</Username>
        /// <Revenue>1000</Revenue>
        /// </Sale>
        /// </remarks>
        /// <returns>A downloadable Excel file with sales data and statistics.</returns>
        /// <response code="200">Returns the sales data Excel file</response>
        /// <response code="401">Unauthorized</response>

        [HttpGet("sales-excel")]
        [SwaggerOperation(Summary = "Get sales data as an Excel file", Description = "Retrieve sales data and statistics in an Excel file.")]
        [SwaggerResponse(200, "Returns the sales data Excel file", typeof(FileResult))]
        [SwaggerResponse(401, "Unauthorized")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> GetSalesExcel()

        {
            var sales = await _context.Sales.ToListAsync();
            using var package = new ExcelPackage();

            // Adding All Sales worksheet
            var allSalesWorksheet = package.Workbook.Worksheets.Add("All Sales");
            allSalesWorksheet.Cells.LoadFromCollection(sales, true);
            FormatWorksheet(allSalesWorksheet);

            // Group by Date
            var salesByDate = sales.GroupBy(s => s.Date.Date)
                       .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), TotalAmount = g.Sum(s => s.Amount), TotalRevenue = g.Sum(s => s.Revenue) })
                       .OrderBy(g => g.Date)
                       .ToList();

            var salesByDateWorksheet = package.Workbook.Worksheets.Add("Sales by Date");
            salesByDateWorksheet.Cells.LoadFromCollection(salesByDate, true);
            FormatWorksheet(salesByDateWorksheet);

            // Group by Username
            var salesByUsername = sales.GroupBy(s => s.Username)
                                       .Select(g => new { Username = g.Key, TotalAmount = g.Sum(s => s.Amount), TotalRevenue = g.Sum(s => s.Revenue) })
                                       .OrderBy(g => g.Username)
                                       .ToList();
            var salesByUsernameWorksheet = package.Workbook.Worksheets.Add("Sales by Username");
            salesByUsernameWorksheet.Cells.LoadFromCollection(salesByUsername, true);
            FormatWorksheet(salesByUsernameWorksheet);

            // Group by Product
            var salesByProduct = sales.GroupBy(s => s.ProductName)
                                      .Select(g => new { ProductName = g.Key, TotalAmount = g.Sum(s => s.Amount), TotalRevenue = g.Sum(s => s.Revenue) })
                                      .OrderBy(g => g.ProductName)
                                      .ToList();
            var salesByProductWorksheet = package.Workbook.Worksheets.Add("Sales by Product");
            salesByProductWorksheet.Cells.LoadFromCollection(salesByProduct, true);
            FormatWorksheet(salesByProductWorksheet);

            // Summary
            var summaryWorksheet = package.Workbook.Worksheets.Add("Summary");
            summaryWorksheet.Cells["A1"].Value = "Total Sales";
            summaryWorksheet.Cells["B1"].Value = sales.Count;
            summaryWorksheet.Cells["A2"].Value = "Total Revenue";
            summaryWorksheet.Cells["B2"].Value = sales.Sum(s => s.Revenue);
            FormatWorksheet(summaryWorksheet);

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = "SalesData.xlsx"
            };
        }

        private void FormatWorksheet(ExcelWorksheet worksheet)
        {
            worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column].Style.Font.Bold = true;
            worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            // Format date columns
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                if (worksheet.Cells[1, col].Text.Contains("Date"))
                {
                    worksheet.Column(col).Style.Numberformat.Format = "yyyy-mm-dd";
                }
            }
        }

    }
}
