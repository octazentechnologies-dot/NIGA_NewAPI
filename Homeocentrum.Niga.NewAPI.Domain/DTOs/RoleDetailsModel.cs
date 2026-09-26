using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RoleDetailsModel
    {
        public int RecordId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int MenuId { get; set; }
        public string MenuName { get; set; }
        public bool? IsView { get; set; }
        public bool? IsAdd { get; set; }
        public bool? IsModify { get; set; }
        public bool? IsDelete { get; set; }
    }
}
