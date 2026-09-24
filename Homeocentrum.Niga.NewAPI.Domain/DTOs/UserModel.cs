using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// This class is used to hold user related data
/// </summary>
namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class UserModel
    {
        public long UserId { get; set; }
        [Required(ErrorMessage = "UserName is Required")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "User Password is Required")]
        public string UserPassword { get; set; }
        public bool UserStatus { get; set; }
        public string UserPhoto { get; set; }
        public string FirmIds { get; set; }
        public string MobileNo { get; set; }
        public string EmailId { get; set; }
        public string OldPassword { get; set; }
        public DateTime PasswordRenewDate { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public bool IsCurrentlyLoggedIn { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public int CountryId { get; set; }
        public int StateId { get; set; }
        public string  EncryptedUserId { get; set; }
        public int RoleId { get; set; }
    }
    public class NewUserModel
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool? UserStatus { get; set; }
        public string EmailId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? RoleId { get; set; }
    }

    public class UserViewModel
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string UserStatus { get; set; }
        public string EmailId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? RoleId { get; set; }
        public string Role { get; set; }
    }
}
