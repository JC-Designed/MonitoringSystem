public class AdminProfileEditModel
{
    public string FullName { get; set; } = string.Empty;   // Full name only
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty; // optional
}