namespace MonitoringSystem.Models
{
    public class RegisterCompanyViewModel
    {
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public string Type { get; set; } // Maps to Industry
        public string SecId { get; set; } // Maps to SecId
        public string Website { get; set; } // Maps to Website
        public string TinId { get; set; } // Maps to TaxId
        public string Description { get; set; } // Maps to CompanyDescription
    }
}