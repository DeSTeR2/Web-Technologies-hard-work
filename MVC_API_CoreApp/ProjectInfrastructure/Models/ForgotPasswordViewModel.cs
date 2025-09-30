using System.ComponentModel.DataAnnotations;

namespace ProjectInfrastructure.Models;

public class ForgotPasswordViewModel
{
    [Required] [DataType(DataType.EmailAddress)]
    public string Email;
}