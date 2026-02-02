using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringSystem.Models.ParamModel
{
    [NotMapped] // 🔥 Important: EF won't treat this as a table
    public class UserAccountModel : BaseResultModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int AccountType { get; set; }
    }

    public class UserAccountParamModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int accountType { get; set; }
        public bool RememberMe { get; set; } = false; // optional
    }
}
