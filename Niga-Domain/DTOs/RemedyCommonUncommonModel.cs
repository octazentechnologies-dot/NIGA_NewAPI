using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class RemedyCommonUncommonModel
    {
        public RemedyCommonUncommonModel()
        {
            this.CommonRemedies = new List<RemediesModel>();
            this.UnCommonRemedies = new List<RemediesModel>();
        }

        public List<RemediesModel> CommonRemedies = new List<RemediesModel>();
        public List<RemediesModel> UnCommonRemedies = new List<RemediesModel>();
    }
}
