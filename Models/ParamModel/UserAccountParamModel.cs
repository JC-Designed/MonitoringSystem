using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models.ParamModel
{
    public class LoginParamModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; } = false; // optional for "keep me logged in"
    }
}