using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class PaginationResult
    {
        public double TotalCount { get; set; } = 0;
        public double TotalPageCount { get; set; } = 0;

        public Object ResultObject { get; set; }
    }
}
