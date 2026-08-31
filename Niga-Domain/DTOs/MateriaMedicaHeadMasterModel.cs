using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class MateriaMedicaHeadMasterModel
    {
        public int MateriaMedicaHeadId { get; set; }
        [Required(ErrorMessage = "Please select AuthorId")]
        public int? AuthorId { get; set; }
        public string MateriaMedicaHeadName { get; set; }
        public string Description { get; set; }
        public bool? IsSection { get; set; }
        public bool? DifferentialMM { get; set; }
        public int? SeqNo { get; set; }
        public bool? IsDeleted { get; set; }

    }
    public class MateriaMedicaHeadMasterModel1
    {
        public int MateriaMedicaHeadId { get; set; }
        [Required(ErrorMessage = "Please select AuthorId")]
        public int? AuthorId { get; set; }
        public string MateriaMedicaHeadName { get; set; }
        public string Description { get; set; }
        public bool? IsSection { get; set; }
        public bool? DifferentialMM { get; set; }
        public int? SeqNo { get; set; }
        public string AuthorName { get; set; }
        public bool? IsDeleted { get; set; }
    }

    public class DifferentialMateriaMedicadDefaultStatusModel
    {
        public int MateriaMedicaHeadId { get; set; }
        public bool DifferentialMM { get; set; }


    }

    public class MateriaMedicaHeadModel
    {
        public int MateriaMedicaHeadId { get; set; } = 0;

        public string MateriaMedicaHeadName { get; set; } = string.Empty;
        public bool? DifferentialMM { get; set; }=false;
    }

    public class DifferentialMateriaMedica
    {

        public DifferentialMateriaMedica()
        {
            this. MateriaMedicaHeadIds = new List<int?>();
            this.RemedyIndexModelList =new List<RemedyIndexModel>();
        }

        public int authorId { get; set; } = 0;

        public List<int?> MateriaMedicaHeadIds { get; set; }
        public List<RemedyIndexModel> RemedyIndexModelList { get; set; }

    }

    public class RemedyIndexModel
    {
        public int remedyId { get; set; } = 0;
        public int index { get; set; } = 0;
        public string score { get; set; } = string.Empty;
    }

    public class DifferentialMateriaMedicaListModel
    {
        public int? RemedyId { get; set; } = 0;
        public string RemedyName { get; set; } = string.Empty;
        public string MateriaMedicaHeadName { get; set; } = string.Empty;
        public string MateriaMedica { get; set; } = string.Empty;
        public string score { get; set; } = string.Empty;

    }
}
