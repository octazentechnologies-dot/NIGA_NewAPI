using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class LocationMasterDto
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public bool IsActive { get; set; }
    }
}