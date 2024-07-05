using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Shop.Models;
using Shop.ViewModels;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shop.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register a new user", Description = "Creates a new user account with the specified username and email. Validates that the username and email are not already in use.")]
        [SwaggerResponse(200, "User created successfully")]
        [SwaggerResponse(400, "Username or email is already taken or validation error occurred")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userByUsername = await _userManager.FindByNameAsync(model.Username);
                var userByEmail = await _userManager.FindByEmailAsync(model.Email);

                if (userByUsername != null)
                {
                    return BadRequest("Username is already taken.");
                }

                if (userByEmail != null)
                {
                    return BadRequest("Email is already in use.");
                }

                var user = new User { UserName = model.Username, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return Ok(new { Result = "User created successfully" });
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }

            return BadRequest(ModelState);
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login a user", Description = "Authenticates a user using the provided username and password. Returns a JWT token if successful.")]
        [SwaggerResponse(200, "User authenticated successfully", Type = typeof(string))]
        [SwaggerResponse(401, "Unauthorized - Invalid username or password")]
        [SwaggerResponse(400, "Validation error occurred")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

                    if (result.Succeeded)
                    {
                        var token = GenerateJwtToken(user);
                        return Ok(new { Token = token });
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return Unauthorized();
                }
            }

            return BadRequest(ModelState);
        }

        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logout the current user", Description = "Logs out the currently authenticated user.")]
        [SwaggerResponse(200, "User logged out successfully")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Result = "User logged out successfully" });
        }

        [HttpGet("user-info")]
        [Authorize]
        [SwaggerOperation(Summary = "Get user information", Description = "Retrieves the information of the currently authenticated user.")]
        [SwaggerResponse(200, "User information retrieved successfully", Type = typeof(UserInfoViewModel))]
        [SwaggerResponse(400, "Unable to get username from JWT")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> GetUserInfo()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Unable to get username from JWT");
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new { Username = user.UserName, Email = user.Email });
        }

        [HttpPut("update-user")]
        [Authorize]
        [SwaggerOperation(Summary = "Update user information", Description = "Updates the information of the currently authenticated user. Validates that the new username and email are not already in use.")]
        [SwaggerResponse(200, "User updated successfully")]
        [SwaggerResponse(400, "Username or email is already taken or validation error occurred")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> UpdateUserInfo(UpdateUserViewModel model)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Unable to get username from JWT");
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (model.Username != null && model.Username != user.UserName)
            {
                var userByUsername = await _userManager.FindByNameAsync(model.Username);
                if (userByUsername != null)
                {
                    return BadRequest("Username is already taken.");
                }
                user.UserName = model.Username;
            }

            if (model.Email != null && model.Email != user.Email)
            {
                var userByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (userByEmail != null)
                {
                    return BadRequest("Email is already in use.");
                }
                user.Email = model.Email;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Result = "User updated successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpDelete("delete-user")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete the current user", Description = "Deletes the account of the currently authenticated user.")]
        [SwaggerResponse(200, "User deleted successfully")]
        [SwaggerResponse(400, "Unable to get username from JWT")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> DeleteUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Unable to get username from JWT");
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Result = "User deleted successfully" });
            }

            return BadRequest(result.Errors);
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class UpdateUserViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class UserInfoViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }
}
