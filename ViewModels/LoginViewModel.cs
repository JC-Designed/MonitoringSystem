namespace MonitoringSystem.ViewModels
{
    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }

        // ✅ Add this property
        public bool RememberMe { get; set; } = false;
    }
}
