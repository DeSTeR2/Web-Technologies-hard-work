using System.ComponentModel.DataAnnotations;

namespace ProjectInfrastructure.Models;

public class RegisterViewModel
{
    [Required] 
    [Display(Name = "Email")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = null!;
    
    [Required]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    public string Passworld { get; set; } = null!; 
        
    [Required]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    public string ConfirmationPassworld { get; set; } = null!; 
}