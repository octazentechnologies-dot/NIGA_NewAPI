using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ModalitiesDetailsModel
    {
        public ModalitiesDetailsModel()
        {
            ModalitiesRubricDetails = new List<ModalitiesRubricDetailsModel>();
        }

        public int ModalitiesDetailsId { get; set; }
        public string ModalitiesDetailsKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }
        public List<ModalitiesRubricDetailsModel> ModalitiesRubricDetails { get; set; }
    }
}
