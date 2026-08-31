using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class NewDetailModel
    {
        public int NewsId { get; set; }
        public int? NewsCategoryId { get; set; }
        public string NewsDate { get; set; }
        public string NewsHeading { get; set; }
        public string NewsCategory1 { get; set; }

        public string NewsSubHeading { get; set; }
        public string NewsContent { get; set; }
        public string NewsImage1 { get; set; }
        public string NewsImage2 { get; set; }
        public string NewsImage3 { get; set; }
        public string NewsImage4 { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public bool? IsActive { get; set; }

    }


    public class NewDetailModel1
    {
        public int NewsId { get; set; }
        public DateTime? NewsDate { get; set; }
        public int? NewsCategoryId { get; set; }
        public string NewsHeading { get; set; }
        public string NewsSubHeading { get; set; }
        public string NewsContent { get; set; }
        public string NewsImage1 { get; set; }
        public string NewsImage2 { get; set; }
        public string NewsImage3 { get; set; }
        public string NewsImage4 { get; set; }
        public int? EnteredBy { get; set; }
        public string NewsCategory1 { get; set; }

        public DateTime? EnteredDate { get; set; }
        public bool? IsActive { get; set; }

        //public UploadImage images { get; set; }
    }



    public class UploadImage
    {
       // public int? NewsId { get; set; }
        public IFormFile NewsImage1 { get; set; }
        public IFormFile NewsImage2 { get; set; }
        public IFormFile NewsImage3 { get; set; }
        public IFormFile NewsImage4 { get; set; }


    }

    public class NewsModel
    {
        public int NewsId { get; set; }
        public string NewsDate { get; set; }
        public string NewsHeading { get; set; }
        public string NewsSubHeading { get; set; }

    }

}
