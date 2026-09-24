using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class CaseDetailsModel
    {

        public CaseDetailsModel()
        {
            this.ModelEx = new List<CaseDetailRemedy1>();
        }
        public int CaseDetailId { get; set; }
        public int? CaseId { get; set; }
        public int? SubsectionId { get; set; }
        public int? IntensityId { get; set; }
        public int? RemedyCount { get; set; }
        public int? RemedyId { get; set; }
        public int? RemedyIndex { get; set; }
        public List<CaseDetailRemedy1> ModelEx { get; set; }
    }


    public class CaseDetailRemedy1
    {

        public int CaseDetailRemedyId { get; set; }
        public int? CaseDetailId { get; set; }
        public int? RemedyId { get; set; }
        public int? RemedyIndex { get; set; }

    }
}
