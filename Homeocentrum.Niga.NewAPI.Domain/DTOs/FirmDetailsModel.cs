using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class FirmDetailsModel
    {
        public int FirmId { get; set; }
        public string FirmName { get; set; }
        public string FirmNameMarathi { get; set; }
        public string FirmRegNumber { get; set; }
        public DateTime FirmRegDate { get; set; }
        public string FirmBranchName { get; set; }
        public string FirmBranchNameMarathi { get; set; }
        public string FirmOfficeAddress { get; set; }
        public string FirmOfficeAddressMarathi { get; set; }
        public string FirmLogo { get; set; }
        public string FirmPhoneNumber { get; set; }
        public string FirmFaxNumber { get; set; }
        public string FirmEmailIid { get; set; }
        public string MailPassword { get; set; }
        public bool IsFederation { get; set; }
        public string FirmConnectionPath { get; set; }
        public string LanguageIds { get; set; }
        public int? ParentFirmId { get; set; }
        public string ModuleIds { get; set; }
        public int? UserLimit { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public bool? IsNeedToBeSingleTerminalLogin { get; set; }
        public string DatabaseBackupPath { get; set; }
        public bool? IsDateOverlap { get; set; }
        public DateOnly? ApplicationLockDate { get; set; }
        public bool? IsLockApplication { get; set; }
        public bool? IsSyncStaring { get; set; }
    }
}
