using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class MateriaMedicaMasterModel
    {
        public MateriaMedicaMasterModel(){
            this.ModelEx =new  List<MateriaMedicaDetailsModel>();
            }

        public int MateriaMedicaId { get; set; }
        public int? AuthorId { get; set; }
        public int? RemedyId { get; set; }
        public int? MateriaMedicaHeadId { get; set; }
        public string Dose { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public int? SeqNo { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
       
        public List<MateriaMedicaDetailsModel> ModelEx{get;set;}
    }
    public class MateriaMedicaDetailsModel
    {
        public int MatriaMedicaDetailId { get; set; }
        public int MateriaMedicaId { get; set; }
        public string Details { get; set; }


    }
    
    public class MateriaMedicaMasterModel1
    {
        public int MateriaMedicaId { get; set; }
        public int? AuthorId { get; set; }
        public int? RemedyId { get; set; }
        public int? MateriaMedicaHeadId { get; set; }
        public string Dose { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public int? SeqNo { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public string AuthorName { get; set; }
        public string RemedyName { get; set; }
        public string MateriaMedicaHeadName { get; set; }
    }

    public class MateriaMedicaMasterModel2
    {
        public int? MateriaMedicaHeadId { get; set; }
        public string MateriaMedicaHeadName { get; set; }

    }

    public class MateriaMedicaModel
    {
        public int MateriaMedicaId { get; set; }
        public int? AuthorId { get; set; }
        public int? RemedyId { get; set; }
        public int? MateriaMedicaHeadId { get; set; }
        public string AuthorName { get; set; }
        public string RemedyName { get; set; }
        public string MateriaMedicaHeadName { get; set; }
    }

    public class MateriaMedicaFilterModel
    {
        public MateriaMedicaFilterModel()
        {
            this.NigaParameter = new NigaParameters();
        }
        public int? AuthorId { get; set; }
        public int? RemedyId { get; set; }

        public NigaParameters NigaParameter { get; set; }
    }

}
