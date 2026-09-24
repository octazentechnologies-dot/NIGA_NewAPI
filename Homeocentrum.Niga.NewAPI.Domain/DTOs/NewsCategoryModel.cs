using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class NewsCategoryModel
    {
        public int NewsCategoryId { get; set; }
        public string NewsCategory1 { get; set; }
        public int? SeqNo { get; set; }
        public bool? IsActive { get; set; }
    }
}
