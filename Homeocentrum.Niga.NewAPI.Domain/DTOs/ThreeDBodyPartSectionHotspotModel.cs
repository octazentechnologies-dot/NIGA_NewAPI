using System;
using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ThreeDBodyPartSectionHotspotModel
    {
        public int SectionHotspotId { get; set; }

        public int SectionId { get; set; }

        public string? SectionName { get; set; }

        public string HotspotName { get; set; } = null!;

        public int? SubSectionId { get; set; }

        public int? EnteredBy { get; set; }

        public DateTime? EnteredDate { get; set; }

        public int? ChangedBy { get; set; }

        public DateTime? ChangedDate { get; set; }

        public bool DeleteStatus { get; set; }
    }

    public class ThreeDBodyPartSectionHotspotListItem
    {
        public int SectionHotspotId { get; set; }

        public int SectionId { get; set; }

        public string? SectionName { get; set; }

        public string HotspotName { get; set; } = null!;

        public int? SubSectionId { get; set; }
    }

    public class AddThreeDBodyPartSectionHotspotRequest
    {
        [Required(ErrorMessage = "SectionID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SectionID is required")]
        public int SectionId { get; set; }

        [Required(ErrorMessage = "HotspotName is required")]
        [MaxLength(200, ErrorMessage = "HotspotName cannot exceed 200 characters")]
        public string HotspotName { get; set; } = null!;

        [Required(ErrorMessage = "EnteredBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "EnteredBy is required")]
        public int EnteredBy { get; set; }

        public int? SubSectionId { get; set; }
    }

    public class UpdateThreeDBodyPartSectionHotspotRequest
    {
        [Required(ErrorMessage = "SectionHotspotID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SectionHotspotID is required")]
        public int SectionHotspotId { get; set; }

        [Required(ErrorMessage = "SectionID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SectionID is required")]
        public int SectionId { get; set; }

        [Required(ErrorMessage = "HotspotName is required")]
        [MaxLength(200, ErrorMessage = "HotspotName cannot exceed 200 characters")]
        public string HotspotName { get; set; } = null!;

        [Required(ErrorMessage = "ChangedBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChangedBy is required")]
        public int ChangedBy { get; set; }

        public int? SubSectionId { get; set; }
    }

    public class DeleteThreeDBodyPartSectionHotspotRequest
    {
        [Required(ErrorMessage = "SectionHotspotID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SectionHotspotID is required")]
        public int SectionHotspotId { get; set; }

        [Required(ErrorMessage = "ChangedBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChangedBy is required")]
        public int ChangedBy { get; set; }
    }

    public class GetThreeDBodyPartSectionHotspotByIdRequest
    {
        [Required(ErrorMessage = "SectionHotspotID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SectionHotspotID is required")]
        public int SectionHotspotId { get; set; }
    }
}
