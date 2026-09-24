using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    #nullable disable
    public class BlogDetailModel
    {
        public int BlogId { get; set; }
        public string BlogHead { get; set; }
        public string BlogSubHead { get; set; }
        public string BlogDate { get; set; }
        public string BlogImage1 { get; set; }
        public string BlogImage2 { get; set; }
        public string BlogDetails1 { get; set; }
        public bool? IsActive { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
    }

    public class BlogDetailModel1
    {
        public int BlogId { get; set; }
        public string BlogHead { get; set; }
        public string BlogSubHead { get; set; }
        public DateTime? BlogDate { get; set; }
        public string BlogImage1 { get; set; }
        public string BlogImage2 { get; set; }
        public string BlogDetails1 { get; set; }
        public bool? IsActive { get; set; }
        public string Src1 { get; set; }
        public string Src2 { get; set; }
    }

    public class BlogDetailViewModel
    {
        public int BlogId { get; set; }
        public string BlogHead { get; set; }
        public string BlogSubHead { get; set; }
        public string BlogDate { get; set; }
    }
}
