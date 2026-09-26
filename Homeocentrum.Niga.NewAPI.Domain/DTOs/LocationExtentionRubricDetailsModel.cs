using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class LocationExtentionRubricDetailsModel
    {
        public int LocationExtentionRubricDetailsId { get; set; }
        public int LocationExtentionDetailsId { get; set; }
        public int SubsectionId { get; set; }
        public int SectionId { get; set; }
        public string SubsectionName { get; set; }
        public string SectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}
