using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class MateriaMedicaRemediesDetailsModel
    {
        public MateriaMedicaRemediesDetailsModel()
        {
            this.lstRemedy = new List<MateriaMedicaRemediesDetailsModel1>();
        }
        public int? RemedyId { get; set; }
        public int? AuthorId { get; set; }
        public List<MateriaMedicaRemediesDetailsModel1> lstRemedy { get; set; }
    }

    public class MateriaMedicaRemediesDetailsModel1
    {
        public int? MateriaMedicaHeadId { get; set; }
        public string MateriaMedicaHeadName { get; set; }
        public string MateriaMedicaDetail1 { get; set; }
    }
}
