using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Shop.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using System.IO;
using SkiaSharp;
using System.Collections.Generic;
using Shop.Models;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesPdfReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalesPdfReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("pdf-report")]
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

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Добавление заголовка
                var title = new Paragraph("Sales Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(24)
                    .SetBold()
                    .SetMarginBottom(20);
                document.Add(title);

                // Создание таблицы продаж
                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 2, 3 })).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Product")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Amount")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Price")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Revenue")).SetBold());
                table.AddHeaderCell(new Cell().Add(new Paragraph("Date")).SetBold());

                foreach (var sale in sales)
                {
                    table.AddCell(new Cell().Add(new Paragraph(sale.ProductName)));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Amount.ToString())));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Price.ToString("C2"))));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Revenue.ToString("C2"))));
                    table.AddCell(new Cell().Add(new Paragraph(sale.Date.ToString("yyyy-MM-dd"))));
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

                // Статистика по продуктам
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

                // Таблица по дням и выручкам за эти дни
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

                // Создание графика по дням и выручкам
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
                document.Close();

                var pdfBytes = memoryStream.ToArray();
                return File(pdfBytes, "application/pdf", "SalesReport.pdf");
            }
        }

        private SKBitmap CreateSalesChart(IEnumerable<Sale> sales)
        {
            var salesData = sales.GroupBy(s => s.Date.Date)
                                 .Select(g => new { Date = g.Key, TotalAmount = g.Sum(s => s.Amount) })
                                 .OrderBy(g => g.Date)
                                 .ToList();

            if (salesData.Count < 2 || salesData.Max(s => s.TotalAmount) == 0)
            {
                // Нет достаточно данных для построения графика
                return null;
            }

            int width = 1200;
            int height = 800;
            var bitmap = new SKBitmap(width, height);
            var canvas = new SKCanvas(bitmap);

            // Цвета для линий
            SKColor lineColor = SKColors.Blue;

            // Настройки для текста
            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true
            };

            // Настройки для линий
            var paint = new SKPaint
            {
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = lineColor
            };

            // Настройки для сетки
            var gridPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                StrokeWidth = 1,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            // Отрисовка заголовка
            textPaint.TextSize = 30;
            canvas.DrawText("Sales Over Time", width / 2 - textPaint.MeasureText("Sales Over Time") / 2, 50, textPaint);

            // Отрисовка осей
            paint.Color = SKColors.Black;
            canvas.DrawLine(100, height - 100, 100, 100, paint); // Y-axis
            canvas.DrawLine(100, height - 100, width - 100, height - 100, paint); // X-axis

            // Подписи осей
            textPaint.TextSize = 24;
            canvas.DrawText("Date", width / 2, height - 50, textPaint);
            canvas.RotateDegrees(-90);
            canvas.DrawText("Total Amount", -height / 2 - textPaint.MeasureText("Total Amount") / 2, 50, textPaint);
            canvas.RotateDegrees(90);

            // Отрисовка сетки
            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawLine(100, height - 100 - i * (height - 200) / 10, width - 100, height - 100 - i * (height - 200) / 10, gridPaint);
                canvas.DrawLine(100 + i * (width - 200) / 10, height - 100, 100 + i * (width - 200) / 10, 100, gridPaint);
            }

            // Отрисовка данных
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

            // Отрисовка меток на оси X
            textPaint.TextSize = 20;
            for (int i = 0; i < salesData.Count; i++)
            {
                canvas.DrawText(salesData[i].Date.ToString("MM-dd"), 100 + i * xStep - textPaint.MeasureText(salesData[i].Date.ToString("MM-dd")) / 2, height - 70, textPaint);
            }

            // Отрисовка меток на оси Y
            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawText((maxAmount * i / 10).ToString("F1"), 50, height - 100 - i * (height - 200) / 10 + 10, textPaint);
            }

            // Подписи к каждой точке
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
                // Нет достаточно данных для построения графика
                return null;
            }

            int width = 1200;
            int height = 800;
            var bitmap = new SKBitmap(width, height);
            var canvas = new SKCanvas(bitmap);

            // Цвета для линий
            SKColor lineColor = SKColors.Green;

            // Настройки для текста
            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true
            };

            // Настройки для линий
            var paint = new SKPaint
            {
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = lineColor
            };

            // Настройки для сетки
            var gridPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                StrokeWidth = 1,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            // Отрисовка заголовка
            textPaint.TextSize = 30;
            canvas.DrawText("Revenue Over Time", width / 2 - textPaint.MeasureText("Revenue Over Time") / 2, 50, textPaint);

            // Отрисовка осей
            paint.Color = SKColors.Black;
            canvas.DrawLine(100, height - 100, 100, 100, paint); // Y-axis
            canvas.DrawLine(100, height - 100, width - 100, height - 100, paint); // X-axis

            // Подписи осей
            textPaint.TextSize = 24;
            canvas.DrawText("Date", width / 2, height - 50, textPaint);
            canvas.RotateDegrees(-90);
            canvas.DrawText("Total Revenue", -height / 2 - textPaint.MeasureText("Total Revenue") / 2, 50, textPaint);
            canvas.RotateDegrees(90);

            // Отрисовка сетки
            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawLine(100, height - 100 - i * (height - 200) / 10, width - 100, height - 100 - i * (height - 200) / 10, gridPaint);
                canvas.DrawLine(100 + i * (width - 200) / 10, height - 100, 100 + i * (width - 200) / 10, 100, gridPaint);
            }

            // Отрисовка данных
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

            // Отрисовка меток на оси X
            textPaint.TextSize = 20;
            for (int i = 0; i < revenueData.Count; i++)
            {
                canvas.DrawText(revenueData[i].Date.ToString("MM-dd"), 100 + i * xStep - textPaint.MeasureText(revenueData[i].Date.ToString("MM-dd")) / 2, height - 70, textPaint);
            }

            // Отрисовка меток на оси Y
            for (int i = 1; i <= 10; i++)
            {
                canvas.DrawText((maxRevenue * i / 10).ToString("F1"), 50, height - 100 - i * (height - 200) / 10 + 10, textPaint);
            }

            // Подписи к каждой точке
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
