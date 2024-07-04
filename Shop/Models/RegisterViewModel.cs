using System.ComponentModel.DataAnnotations;

namespace Shop.ViewModels
{
    /// <summary>
    /// ViewModel for user registration.
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
