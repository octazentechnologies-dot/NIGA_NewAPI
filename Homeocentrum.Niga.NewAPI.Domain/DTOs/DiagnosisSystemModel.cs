using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class DiagnosisSystemModel
    {
        public int DiagnosisSystemId { get; set; }

        public string DiagnosisSystemName { get; set; }
       
        public string Description { get; set; }
        public bool IsActive { get; set; }


    }
}
