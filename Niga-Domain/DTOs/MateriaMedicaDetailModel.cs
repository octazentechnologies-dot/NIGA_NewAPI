using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class MateriaMedicaDetailModel
    {
        public int MatriaMedicaDetailId { get; set; }
        [Required(ErrorMessage = "Please select MateriMedica")]
        public int MateriaMedicaId { get; set; }
        public string MateriaMedicaDetail1 { get; set; }
        public int? SeqNo { get; set; }

    }
}
