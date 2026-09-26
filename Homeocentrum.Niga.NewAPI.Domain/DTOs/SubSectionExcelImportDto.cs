using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class SubSectionExcelImportDto
    {
        public int SectionId { get; set; }
        public string SubSectionName { get; set; }
        public string SubSectionNameAlias { get; set; }
        public string Description { get; set; }
        public int? ParentSubSectionId { get; set; }
        public string LanguageName { get; set; }
        public string SubSectionDetails { get; set; }
        public List<ReferenceRubricDto> Referencerubric { get; set; }
        public int EnteredBy { get; set; }
    }

    public class SubLanguageDetailDto
    {
        public int LanguageId { get; set; }
        public string SubSectionDetails { get; set; }
    }

    public class ReferenceRubricDto 
    {
        public int SubSectionId { get; set; }
        public List<int> RefSubSectionId { get; set; }
    }
}