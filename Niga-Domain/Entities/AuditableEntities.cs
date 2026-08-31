using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Niga_Domain.Entities
{
    public class AuditableEntities
    {
        public string? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }

    }
}