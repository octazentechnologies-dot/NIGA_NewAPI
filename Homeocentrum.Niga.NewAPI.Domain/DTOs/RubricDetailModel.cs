using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RubricDetailModel
    {
        public RubricDetailModel()
        {
            this.Referencerubric = new List<ReferenceRubricDetailsModel>();
            this.SubSectionLanguageDetails = new List<SubSectionLanguageDetailsModel>();
            this.RemediesList = new List<RemediesModel>();
        }
        public int SubSectionId { get; set; } = 0;
        public int? SectionId { get; set; } = 0;
        public string SectionName { get; set; }=string.Empty;

        public int? ParentSubSectionId { get; set; } = 0;
        public string SubSectionName { get; set; } = string.Empty;
        public string SubSectionNameAlias { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int RemdeyCount { get; set; } = 0;
        public List<ReferenceRubricDetailsModel> Referencerubric { get; set; }

        public List<SubSectionLanguageDetailsModel> SubSectionLanguageDetails { get; set; }

        public List<RemediesModel> RemediesList { get; set; }
    }
}
