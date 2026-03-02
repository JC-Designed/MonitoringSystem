//namespace MonitoringSystem.Models
//{
//    public class UserAccount
//    {
//        public int UserID { get; set; }
//        public string Username { get; set; } = string.Empty;
//        public string Password { get; set; } = string.Empty;
//        public int AccountTypeID { get; set; }

//        // Navigation property
//        public AccountType AccountType { get; set; } = null!;
//        public DateTime CreatedDate { get; set; }
//        public DateTime? LastLogin { get; set; }
//    }

//    public class AccountType
//    {
//        // ✅ EF needs a primary key
//        public int TypeId { get; set; }

//        public string TypeName { get; set; } = string.Empty;

//        // ✅ Optional navigation back to users
//        public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
//    }
//}