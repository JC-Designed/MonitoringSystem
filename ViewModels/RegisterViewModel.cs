// Models/ViewModels/RegisterViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MonitoringSystem.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Student ID is required")]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Birth month is required")]
        public int BirthMonth { get; set; }

        [Required(ErrorMessage = "Birth day is required")]
        public int BirthDay { get; set; }

        [Required(ErrorMessage = "Birth year is required")]
        public int BirthYear { get; set; }

        [Required(ErrorMessage = "You must agree to the terms")]
        public bool Terms { get; set; }

        // Computed property for birth date
        public DateTime BirthDate => new DateTime(BirthYear, BirthMonth, BirthDay);
    }
}