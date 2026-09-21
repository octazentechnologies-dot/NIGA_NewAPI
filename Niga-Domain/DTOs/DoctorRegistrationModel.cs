using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Niga_Domain.DTOs
{
    /// <summary>
    /// Public doctor self-registration payload (package/subscription selected after login).
    /// </summary>
    public class DoctorRegistrationModel
    {
        [Required(ErrorMessage = "First Name is required")]
        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "User Name is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Valid Email is required")]
        public string EmailId { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        public string MobileNo { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters")]
        public string UserPassword { get; set; }

        [Required(ErrorMessage = "Clinic / Company name is required")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Country is required")]
        public int CountryId { get; set; }

        public int? StateId { get; set; }

        public string City { get; set; }

        public string PermanantAddress { get; set; }

        [Required(ErrorMessage = "Qualification is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Qualification is required")]
        public int QualificationId { get; set; }

        public string PassingUniversity { get; set; }

        public string PassingCertNo { get; set; }
    }

    /// <summary>
    /// WEB-09.03 — RegisterDoctor fields plus qualification/registration files.
    /// Single [FromForm] model so Swagger does not collapse two IFormFile parameters
    /// onto a duplicate ContentType key.
    /// </summary>
    public class RegisterDoctorWithDocumentsForm : DoctorRegistrationModel
    {
        public IFormFile QualificationDoc { get; set; }

        public IFormFile RegistrationDoc { get; set; }
    }
}
