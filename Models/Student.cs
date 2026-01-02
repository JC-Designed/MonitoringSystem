public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int SchoolYear { get; set; } // optional if you want to filter by school year
}
