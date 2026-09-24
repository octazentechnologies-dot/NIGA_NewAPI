using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
#nullable disable
    public class SubSectionModel : AuditableEntities
    {
        // public SubSectionModel()
        // {
        //     this.Referencerubric = new List<ReferenceRubricDetailsModel>();

        //     this.SubSectionLanguageDetails = new List<SubSectionLanguageDetailsModel>();
        // }
        public int SubSectionId { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int? ParentSubSectionId { get; set; }
        public string ParentSubSectionName { get; set; } = string.Empty;
        [Required(ErrorMessage = "SubSection Name is required")]
        public string SubSectionName { get; set; }

        public string SubSectionNameAlias { get; set; }
        public string Description { get; set; }
        public bool DeleteStatus { get; set; }
        public List<ReferenceRubricDetailsModel> Referencerubric { get; set; }
        public List<SubSectionLanguageDetailsModel> SubSectionLanguageDetails { get; set; }
    }



    public class ReferenceRubricDetailsModel
    {
        public int ReferenceRubricId { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int? SubSectionId { get; set; }
        public int? RefSubSectionId { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool? DeleteStatus { get; set; }
        public string RefSubSectionName { get; set; }

    }
    public class SubSectionLanguageDetailsModel
    {
        public int SubSectionLanguageId { get; set; }
        public int SubSectionId { get; set; }
        public int LanguageId { get; set; }
        public string SubSectionDetails { get; set; }
        public string SectionName { get; set; }
        public string LanguageName { get; set; }
        public string LanguageDescription { get; set; }


    }
    public class SubSection
    {
        public int SubSectionId { get; set; }
        public int? SectionId { get; set; }
        public int? ParentSubSectionId { get; set; }
        [Required(ErrorMessage = "SubSection Name is required")]
        public string SubSectionName { get; set; }
        [Required(ErrorMessage = "SubSection Name Alias is required")]
        public string SubSectionNameAlias { get; set; }
        public string Description { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int UserId { get; set; }
        public int RemedyId { get; set; }
        public string RemedyName { get; set; }
        public int? GradeId { get; set; }
        public double RemedyCount { get; set; }

    }

    public class SubSectionForPageModel
    {
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; }
    }

    /// <summary>
    /// Query parameters for searching subsections by hotspot name.
    /// </summary>
    public class SearchSubSectionByHotspotRequest
    {
        [Required(ErrorMessage = "HotspotName is required")]
        public string HotspotName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, PaginationRequestModel.MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }

    public class SubSectionViewModel
    {
        public int SubSectionId { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int? ParentSubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public string SubSectionNameAlias { get; set; }
        public string Description { get; set; }
    }


    public class SubSectionList
    {
        public int SubSectionId { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int? ParentSubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public string SubSectionNameAlias { get; set; }
        public string Description { get; set; }
        public string ParentSubSectionName { get; set; }
    }

    public class SubSectionDDModel
    {
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; }
    }


    public class AddSubSectionModel
    {
        public int SubSectionId { get; set; }
        public int? SectionId { get; set; }
        public int? ParentSubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public string SubSectionNameAlias { get; set; }
        public string Description { get; set; }
        public bool DeleteStatus { get; set; }
        public List<AddReferenceRubricDetails> Referencerubric { get; set; }
        public List<AddSubSectionLanguage> SubLanguageDetail { get; set; }
    }

    public class AddReferenceRubricDetails
    {
        public int ReferenceRubricId { get; set; }
        public int? SubSectionId { get; set; }
        public List<int?> RefSubSectionId { get; set; }
    }
    public class AddSubSectionLanguage
    {
        public int SubSectionLanguageId { get; set; }
        public int SubSectionId { get; set; }
        public int LanguageId { get; set; }
        public string SubSectionDetails { get; set; }
        public bool? DeleteStatus { get; set; }
    }
    
    public class SubSectionDDLModel
    {
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; }
    }


    public class RubricImportRow
    {
        public string SectionName { get; set; }
        public string MainRubric { get; set; }
        public string MainParent { get; set; } // Y / N
        public string ParentRubric { get; set; }
        public string CrossReference { get; set; }
        public string EnglishMeaning { get; set; }
        public string MarathiMeaning { get; set; }
    }

    public class RubricImportResult
    {
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int SkippedRows { get; set; }
        public string SkippedFilePath { get; set; }
    }



}
