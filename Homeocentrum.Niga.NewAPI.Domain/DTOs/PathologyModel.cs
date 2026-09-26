using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class PathologyModel
    {
        public int PathologyId { get; set; }
        public string PathologyName { get; set; }
        public string Description { get; set; }
        public bool? DeleteStatus { get; set; }
    }
}
