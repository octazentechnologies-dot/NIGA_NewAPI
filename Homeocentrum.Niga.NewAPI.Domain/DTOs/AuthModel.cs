using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class AuthModel
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsSuperUser { get; set; }
        public string  Role { get; set; }
        public string Token { get; set; }
        public string FirmIds { get; set; }
        public int? RoleId { get; set; }

        public int? DoctorId { get; set; }

        public int? ReceptionStaffId { get; set; }

        public bool IsPlanActive { get; set; }
        public bool IslastFiveDays { get; set; }

        public int DaysRemaining { get; set; }
    }
}
