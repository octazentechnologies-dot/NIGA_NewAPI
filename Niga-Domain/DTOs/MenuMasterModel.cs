using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class MenuMasterModel
    {
        public int MenuId { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string MenuName { get; set; }
        public string MenuNameMarathi { get; set; }
        public string MenuType { get; set; }
        public int? ParentMenuId { get; set; }
        public string MenuUrl { get; set; }
        public string Description { get; set; }
        public string MenuIcon { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public bool? IsLeaf { get; set; }
        public bool? ShowInMainMenu { get; set; }
        public int? SeqNo { get; set; }
        public string FirmIds { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }

    public class MenuMasterResModel
    {
        public int MenuId { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string MenuName { get; set; }
        public string MenuNameMarathi { get; set; }
        public string MenuType { get; set; }
        public int? ParentMenuId { get; set; }
        public string MenuUrl { get; set; }
        public string Description { get; set; }
        public string MenuIcon { get; set; }
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public bool? IsLeaf { get; set; }
        public bool? ShowInMainMenu { get; set; }
        public int? SeqNo { get; set; }
        public string FirmIds { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public bool? IsView { get; set; }
        public bool? IsAdd { get; set; }
        public bool? IsModify { get; set; }

    }
}
