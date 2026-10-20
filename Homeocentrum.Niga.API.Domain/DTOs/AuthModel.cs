using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class AuthModel
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public bool IsSuperUser { get; set; }
        public string  Role { get; set; }
        public string Token { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string FirmIds { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LoginId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Email { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MobileNo { get; set; }
        public int? RoleId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DoctorId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ReceptionStaffId { get; set; }

        public bool IsPlanActive { get; set; }
        public bool IslastFiveDays { get; set; }

        public int DaysRemaining { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DoctorUserId { get; set; }
    }
}
