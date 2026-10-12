using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DrugSystemModel
    {
        public int DrugSystemId { get; set; }
        public string DrugSystemName { get; set; }
        public bool? DeleteStatus { get; set; }
    }
}
