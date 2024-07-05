using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data;
using Shop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SalesController> _logger;

        public SalesController(ApplicationDbContext context, ILogger<SalesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetUsernameFromToken()
        {
            try
            {
                var identity = HttpContext.User.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    var userClaim = identity.FindFirst(ClaimTypes.Name);
                    if (userClaim != null)
                    {
                        return userClaim.Value;
                    }
                    else
                    {
                        _logger.LogError("User claim is null");
                    }
                }
                else
                {
                    _logger.LogError("Identity is null");
                }
                throw new Exception("Unable to get username from JWT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting username from JWT");
                throw new Exception("Unable to get username from JWT", ex);
            }
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all sales", Description = "Retrieve all sales records ordered by date.")]
        [SwaggerResponse(200, "Returns the list of sales", typeof(IEnumerable<Sale>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSales()
        {
            return await _context.Sales.OrderBy(s => s.Date).ToListAsync();
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a sale by ID", Description = "Retrieve a specific sale record by its ID.")]
        [SwaggerResponse(200, "Returns the sale record", typeof(Sale))]
        [SwaggerResponse(404, "Sale not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<ActionResult<Sale>> GetSale(int id)
        {
            var sale = await _context.Sales.FindAsync(id);

            if (sale == null)
            {
                return NotFound();
            }

            return sale;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create a new sale", Description = "Create a new sale record. The username is automatically filled from the JWT token.")]
        [SwaggerResponse(201, "Sale created successfully", typeof(Sale))]
        [SwaggerResponse(400, "Invalid input or username not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<ActionResult<Sale>> PostSale(Sale sale)
        {
            try
            {
                var username = GetUsernameFromToken();
                sale.Username = username;

                var userExists = await _context.Users.AnyAsync(u => u.UserName == username);
                if (!userExists)
                {
                    return BadRequest("Invalid username.");
                }

                sale.Date = DateTime.SpecifyKind(sale.Date, DateTimeKind.Utc);
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetSale", new { id = sale.Id }, sale);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a sale", Description = "Delete a specific sale record by its ID.")]
        [SwaggerResponse(204, "Sale deleted successfully")]
        [SwaggerResponse(404, "Sale not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
            {
                return NotFound();
            }

            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
