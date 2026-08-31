using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class LocationExtentionDetailsModel
    {
        public LocationExtentionDetailsModel()
        {
            LocationExtentionRubricDetails = new List<LocationExtentionRubricDetailsModel>();
        }

        public int LocationExtentionDetailsId { get; set; }
        public string LocationExtentionDetailsKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public virtual List<LocationExtentionRubricDetailsModel> LocationExtentionRubricDetails { get; set; }

    }
}
