using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ModalitiesRubricDetailsModel
    {
        public int ModalitiesRubricDetailsId { get; set; }
        public int ModalitiesDetailsId { get; set; }
        public int? SectionId { get; set; }
        public int? SubsectionId { get; set; }
        public string SectionName { get; set; }
        public string SubsectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}
