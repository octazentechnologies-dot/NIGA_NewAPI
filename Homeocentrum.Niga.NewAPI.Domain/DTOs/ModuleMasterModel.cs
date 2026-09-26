using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ModuleMasterModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleMarathiName { get; set; }
        public string ModuleIcon { get; set; }
        public string ModuleAreaName { get; set; }
        public int Seqno { get; set; }
        public bool IsDirectNode { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public string ModuleUrl { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }
}
