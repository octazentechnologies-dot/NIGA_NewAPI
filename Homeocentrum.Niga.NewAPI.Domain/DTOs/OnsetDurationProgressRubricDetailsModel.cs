using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class OnsetDurationProgressRubricDetailsModel
    {
        public int OnsetRubricId { get; set; }
        public int OnsetDetailId { get; set; }
        public int SubsectionId { get; set; }
        public string SubsectionName { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}
