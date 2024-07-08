using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using SkiaSharp;
using Shop.Models;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesPdfReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the SalesPdfReportController class.
        /// </summary>
        /// <param name="context">The database context to be used.</param>
        public SalesPdfReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Generates a PDF report of sales data.
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves sales data from the database and generates a PDF report.
        /// </remarks>
        /// <returns>A PDF file containing the sales report.</returns>
        /// <response code="200">Returns the generated PDF file.</response>
        /// <response code="401">Unauthorized access.</response>
        [HttpGet("generate-report")]
        [SwaggerOperation(Summary = "Generate sales PDF report", Description = "Retrieve sales data and generate a PDF report.")]
        [SwaggerResponse(200, "Returns the generated PDF file", typeof(FileResult))]
        [Produces("application/octet-stream")]

        public async Task<IActionResult> GetPdfReport()
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

            var topSellers = sales.GroupBy(s => s.Username)
                                  .Select(g => new { Username = g.Key, TotalRevenue = g.Sum(s => s.Revenue) })
                                  .OrderByDescending(g => g.TotalRevenue)
                                  .Take(3)
                                  .ToList();

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                var title = new Paragraph("Sales Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(24)
                    .SetBold()
                    .SetMarginBottom(20);
                document.Add(title);

                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 2, 3, 2 })).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Product")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Amount")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Price")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Revenue")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Date")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Username")).SetBold());

                foreach (var sale in sales)
                {
                    table.AddCell(new Cell().Add(new Paragraph(sale.ProductName)));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Amount.ToString())));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Price.ToString("C2"))));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Revenue.ToString("C2"))));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Date.ToString("yyyy-MM-dd"))));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Username)));
                }

                table.SetBorder(new SolidBorder(1));
                table.SetMarginBottom(20);
                document.Add(table);

                if (groupedByDate != null)
                {
                    document.Add(new Paragraph($"\nDate with highest sales: {groupedByDate.Key}, Sales Count: {groupedByDate.Count()}").SetBold().SetMarginBottom(20));
                }

                document.Add(new Paragraph($"\nTotal Revenue: {totalRevenue.ToString("C2")}").SetBold().SetMarginBottom(20));

                if (highestRevenueDay != null)
                {
                    document.Add(new Paragraph($"\nDay with highest revenue: {highestRevenueDay.Date}, Revenue: {highestRevenueDay.TotalRevenue.ToString("C2")}").SetBold().SetMarginBottom(20));
                }

                var productStatisticsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 3 })).UseAllAvailableWidth();
                productStatisticsTable.AddHeaderCell(new Cell().Add(new Paragraph("Product")).SetBold());
                productStatisticsTable.AddHeaderCell(new Cell().Add(new Paragraph("Total Amount")).SetBold());
                productStatisticsTable.AddHeaderCell(new Cell().Add(new Paragraph("Total Revenue")).SetBold());

                foreach (var product in groupedByProduct)
                {
                    productStatisticsTable.AddCell(new Cell().Add(new Paragraph(product.ProductName)));
                    productStatisticsTable.AddCell(new Cell().Add(new Paragraph(product.TotalAmount.ToString())));
                    productStatisticsTable.AddCell(new Cell().Add(new Paragraph(product.TotalRevenue.ToString("C2"))));
                }

                productStatisticsTable.SetBorder(new SolidBorder(1));
                productStatisticsTable.SetMarginBottom(20);
                document.Add(productStatisticsTable);

                var dailyRevenueTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 3 })).UseAllAvailableWidth();
                dailyRevenueTable.AddHeaderCell(new Cell().Add(new Paragraph("Date")).SetBold());
                dailyRevenueTable.AddHeaderCell(new Cell().Add(new Paragraph("Total Revenue")).SetBold());

                foreach (var day in sales.GroupBy(s => s.Date.Date).Select(g => new { Date = g.Key, TotalRevenue = g.Sum(s => s.Revenue) }))
                {
                    dailyRevenueTable.AddCell(new Cell().Add(new Paragraph(day.Date.ToString("yyyy-MM-dd"))));
                    dailyRevenueTable.AddCell(new Cell().Add(new Paragraph(day.TotalRevenue.ToString("C2"))));
                }

                dailyRevenueTable.SetBorder(new SolidBorder(1));
                dailyRevenueTable.SetMarginBottom(20);
                document.Add(dailyRevenueTable);

                document.Add(new Paragraph("\nTop Sellers:").SetBold().SetMarginBottom(10));
                var topSellersTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 3 })).UseAllAvailableWidth();
                topSellersTable.AddHeaderCell(new Cell().Add(new Paragraph("Username")).SetBold());
                topSellersTable.AddHeaderCell(new Cell().Add(new Paragraph("Total Revenue")).SetBold());

                foreach (var seller in topSellers)
                {
                    topSellersTable.AddCell(new Cell().Add(new Paragraph(seller.Username)));
                    topSellersTable.AddCell(new Cell().Add(new Paragraph(seller.TotalRevenue.ToString("C2"))));
                }

                topSellersTable.SetBorder(new SolidBorder(1));
                topSellersTable.SetMarginBottom(20);
                document.Add(topSellersTable);

                var chartImage = CreateSalesChart(sales);
                if (chartImage != null)
                {
                    var imageData = iText.IO.Image.ImageDataFactory.Create(chartImage.Encode(SKEncodedImageFormat.Png, 100).ToArray());
                    var image = new iText.Layout.Element.Image(imageData).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    document.Add(image);
                }
                else
                {
                    document.Add(new Paragraph("No sales data available to generate chart.").SetBold().SetTextAlignment(TextAlignment.CENTER));
                }

                var chartImageRevenue = CreateRevenueChart(sales);
                if (chartImageRevenue != null)
                {
                    var imageData = iText.IO.Image.ImageDataFactory.Create(chartImageRevenue.Encode(SKEncodedImageFormat.Png, 100).ToArray());
                    var image = new iText.Layout.Element.Image(imageData).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    document.Add(image);
                }
                else
                {
                    document.Add(new Paragraph("No sales data available to generate chart.").SetBold().SetTextAlignment(TextAlignment.CENTER));
                }

                var sellerChartImage = CreateSellerChart(sales);
                if (sellerChartImage != null)
                {
                    var sellerChartImageData = iText.IO.Image.ImageDataFactory.Create(sellerChartImage.Encode(SKEncodedImageFormat.Png, 100).ToArray());
                    var sellerChart = new iText.Layout.Element.Image(sellerChartImageData).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    document.Add(sellerChart);
                }
                else
                {
                    document.Add(new Paragraph("No sales data available to generate seller chart.").SetBold().SetTextAlignment(TextAlignment.CENTER));
                }

                document.Close();

                var pdfBytes = memoryStream.ToArray();
                return File(pdfBytes, "application/pdf", "SalesReport.pdf");
            }
        }

        private SKBitmap CreateSellerChart(IEnumerable<Sale> sales)
        {
            var sellerData = sales.GroupBy(s => s.Username)
                                  .Select(g => new { Username = g.Key, TotalRevenue = g.Sum(s => s.Revenue) })
                                  .OrderByDescending(g => g.TotalRevenue)
                                  .Take(10)
                                  .ToList();

            if (sellerData.Count == 0)
            {
                return null;
            }

            int width = 1200;
            int height = 800;
            var bitmap = new SKBitmap(width, height);
            var canvas = new SKCanvas(bitmap);

            SKColor barColor = SKColors.CornflowerBlue;

            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true
            };

            var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = barColor
            };

            var gridPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                StrokeWidth = 1,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            textPaint.TextSize = 30;
            canvas.DrawText("Top Sellers by Revenue", width / 2 - textPaint.MeasureText("Top Sellers by Revenue") / 2, 50, textPaint);

            paint.Color = SKColors.Black;
            canvas.DrawLine(100, height - 100, 100, 100, paint);
            canvas.DrawLine(100, height - 100, width - 100, height - 100, paint);

            textPaint.TextSize = 24;
            canvas.DrawText("Username", width / 2, height - 50, textPaint);
            canvas.RotateDegrees(-90);
            canvas.DrawText("Total Revenue", -height / 2 - textPaint.MeasureText("Total Revenue") / 2, 50, textPaint);
            canvas.RotateDegrees(90);

            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawLine(100, height - 100 - i * (height - 200) / 10, width - 100, height - 100 - i * (height - 200) / 10, gridPaint);
            }

            float maxRevenue = (float)sellerData.Max(s => s.TotalRevenue);
            float barWidth = (width - 200) / (float)sellerData.Count;
            float yStep = (height - 200) / maxRevenue;

            for (int i = 0; i < sellerData.Count; i++)
            {
                float barHeight = (float)sellerData[i].TotalRevenue * yStep;
                canvas.DrawRect(100 + i * barWidth, height - 100 - barHeight, barWidth - 10, barHeight, paint);

                textPaint.TextSize = 18;
                textPaint.Color = SKColors.Black;
                var label = sellerData[i].Username;
                canvas.DrawText(label, 100 + i * barWidth + (barWidth - 10) / 2 - textPaint.MeasureText(label) / 2, height - 70, textPaint);

                var valueLabel = sellerData[i].TotalRevenue.ToString("C2");
                canvas.DrawText(valueLabel, 100 + i * barWidth + (barWidth - 10) / 2 - textPaint.MeasureText(valueLabel) / 2, height - 110 - barHeight, textPaint);
            }

            return bitmap;
        }

        private SKBitmap CreateSalesChart(IEnumerable<Sale> sales)
        {
            var salesData = sales.GroupBy(s => s.Date.Date)
                                 .Select(g => new { Date = g.Key, TotalAmount = g.Sum(s => s.Amount) })
                                 .OrderBy(g => g.Date)
                                 .ToList();

            if (salesData.Count < 2 || salesData.Max(s => s.TotalAmount) == 0)
            {
                return null;
            }

            int width = 1200;
            int height = 800;
            var bitmap = new SKBitmap(width, height);
            var canvas = new SKCanvas(bitmap);

            SKColor lineColor = SKColors.Blue;

            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true
            };

            var paint = new SKPaint
            {
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = lineColor
            };

            var gridPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                StrokeWidth = 1,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            textPaint.TextSize = 30;
            canvas.DrawText("Sales Over Time", width / 2 - textPaint.MeasureText("Sales Over Time") / 2, 50, textPaint);

            paint.Color = SKColors.Black;
            canvas.DrawLine(100, height - 100, 100, 100, paint);
            canvas.DrawLine(100, height - 100, width - 100, height - 100, paint);

            textPaint.TextSize = 24;
            canvas.DrawText("Date", width / 2, height - 50, textPaint);
            canvas.RotateDegrees(-90);
            canvas.DrawText("Total Amount", -height / 2 - textPaint.MeasureText("Total Amount") / 2, 50, textPaint);
            canvas.RotateDegrees(90);

            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawLine(100, height - 100 - i * (height - 200) / 10, width - 100, height - 100 - i * (height - 200) / 10, gridPaint);
                canvas.DrawLine(100 + i * (width - 200) / 10, height - 100, 100 + i * (width - 200) / 10, 100, gridPaint);
            }

            float maxAmount = (float)salesData.Max(s => s.TotalAmount);
            float xStep = (width - 200) / (float)(salesData.Count - 1);
            float yStep = (height - 200) / maxAmount;

            var points = new SKPoint[salesData.Count];
            for (int i = 0; i < salesData.Count; i++)
            {
                points[i] = new SKPoint(100 + i * xStep, height - 100 - (float)salesData[i].TotalAmount * yStep);
            }

            paint.Color = lineColor;
            canvas.DrawPoints(SKPointMode.Polygon, points, paint);

            textPaint.TextSize = 20;
            for (int i = 0; i < salesData.Count; i++)
            {
                canvas.DrawText(salesData[i].Date.ToString("MM-dd"), 100 + i * xStep - textPaint.MeasureText(salesData[i].Date.ToString("MM-dd")) / 2, height - 70, textPaint);
            }

            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawText((maxAmount * i / 10).ToString("F1"), 50, height - 100 - i * (height - 200) / 10 + 10, textPaint);
            }

            textPaint.TextSize = 18;
            textPaint.Color = SKColors.Red;
            for (int i = 0; i < salesData.Count; i++)
            {
                var point = points[i];
                var label = salesData[i].TotalAmount.ToString("F2");
                canvas.DrawText(label, point.X - textPaint.MeasureText(label) / 2, point.Y - 10, textPaint);
            }

            return bitmap;
        }

        private SKBitmap CreateRevenueChart(IEnumerable<Sale> sales)
        {
            var revenueData = sales.GroupBy(s => s.Date.Date)
                                   .Select(g => new { Date = g.Key, TotalRevenue = g.Sum(s => s.Amount * s.Price) })
                                   .OrderBy(g => g.Date)
                                   .ToList();

            if (revenueData.Count < 2 || revenueData.Max(s => s.TotalRevenue) == 0)
            {
                return null;
            }

            int width = 1200;
            int height = 800;
            var bitmap = new SKBitmap(width, height);
            var canvas = new SKCanvas(bitmap);

            SKColor lineColor = SKColors.Green;

            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true
            };

            var paint = new SKPaint
            {
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = lineColor
            };

            var gridPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                StrokeWidth = 1,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            textPaint.TextSize = 30;
            canvas.DrawText("Revenue Over Time", width / 2 - textPaint.MeasureText("Revenue Over Time") / 2, 50, textPaint);

            paint.Color = SKColors.Black;
            canvas.DrawLine(100, height - 100, 100, 100, paint);
            canvas.DrawLine(100, height - 100, width - 100, height - 100, paint);

            textPaint.TextSize = 24;
            canvas.DrawText("Date", width / 2, height - 50, textPaint);
            canvas.RotateDegrees(-90);
            canvas.DrawText("Total Revenue", -height / 2 - textPaint.MeasureText("Total Revenue") / 2, 50, textPaint);
            canvas.RotateDegrees(90);

            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawLine(100, height - 100 - i * (height - 200) / 10, width - 100, height - 100 - i * (height - 200) / 10, gridPaint);
                canvas.DrawLine(100 + i * (width - 200) / 10, height - 100, 100 + i * (width - 200) / 10, 100, gridPaint);
            }

            float maxRevenue = (float)revenueData.Max(s => s.TotalRevenue);
            float xStep = (width - 200) / (float)(revenueData.Count - 1);
            float yStep = (height - 200) / maxRevenue;

            var points = new SKPoint[revenueData.Count];
            for (int i = 0; i < revenueData.Count; i++)
            {
                points[i] = new SKPoint(100 + i * xStep, height - 100 - (float)revenueData[i].TotalRevenue * yStep);
            }

            paint.Color = lineColor;
            canvas.DrawPoints(SKPointMode.Polygon, points, paint);

            textPaint.TextSize = 20;
            for (int i = 0; i < revenueData.Count; i++)
            {
                canvas.DrawText(revenueData[i].Date.ToString("MM-dd"), 100 + i * xStep - textPaint.MeasureText(revenueData[i].Date.ToString("MM-dd")) / 2, height - 70, textPaint);
            }

            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawText((maxRevenue * i / 10).ToString("F1"), 50, height - 100 - i * (height - 200) / 10 + 10, textPaint);
            }

            textPaint.TextSize = 18;
            textPaint.Color = SKColors.Red;
            for (int i = 0; i < revenueData.Count; i++)
            {
                var point = points[i];
                var label = revenueData[i].TotalRevenue.ToString("F2");
                canvas.DrawText(label, point.X - textPaint.MeasureText(label) / 2, point.Y - 10, textPaint);
            }

            return bitmap;
            }

        }
    }
