using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class MonoGramModel
    {
        public int MonogramId { get; set; }
        public string Monogram1 { get; set; }
        public string Keywords { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool? IsActive { get; set; }

        public List<MonoGramDetailsListModel> ModelEx { get; set; } = new List<MonoGramDetailsListModel>();


    }
}
