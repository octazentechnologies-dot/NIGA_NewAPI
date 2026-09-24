using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ObservationsRubricDetailsModel
    {
        public int ObservationsRubricDetailsId { get; set; }
        public int ObservationsDetailsId { get; set; }
        public int SectionId { get; set; }
        public int Subsection { get; set; }
        public string SectionName { get; set; }
        public string SubsectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}
