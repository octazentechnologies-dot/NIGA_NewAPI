using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ObservationsDetailsModel
    {
        public ObservationsDetailsModel()
        {
            ObservationsRubricDetails = new List<ObservationsRubricDetailsModel>();
        }

        public int ObservationsDetailsId { get; set; }
        public string ObservationsDetailsKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public List<ObservationsRubricDetailsModel> ObservationsRubricDetails { get; set; }
    }
}
