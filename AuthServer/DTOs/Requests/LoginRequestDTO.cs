using System.ComponentModel.DataAnnotations;

namespace AuthServer.DTOs.Requests
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "ClientId is required.")]
        [StringLength(100, ErrorMessage = "ClientId cannot exceed 100 characters.")]
        public string ClientId { get; set; } = default!;

        [Required(ErrorMessage = "ClientSecret is required.")]
        [StringLength(100, ErrorMessage = "ClientSecret cannot exceed 100 characters.")]
        public string ClientSecret { get; set; } = default!;

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100, ErrorMessage = "Please enter valid Email address")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must between 6 and 100 charactres")]
        public string Password { get; set; } = default!;

        [Required(ErrorMessage = "DeviceId is required.")]
        [StringLength(100, ErrorMessage ="DeviceId cannot exceed 100 characters")]
        public string DeviceId { get; set; } = default!;
    }
}
