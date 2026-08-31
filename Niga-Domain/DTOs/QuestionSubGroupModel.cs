using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
  public class QuestionSubGroupModel
    {
        public int QuestionSubgroupId { get; set; }
        public string QuestionSubGroupName { get; set; }
        public int? QuestionGroupId { get; set; }
        public string QuestionGroupName { get; set; }
        public string Description { get; set; }
        public bool? DeleteStatus { get; set; }
    }

    public class QuestionSubGroupModelDDL
    {
        public int QuestionSubgroupId { get; set; }
        public string QuestionSubgroup1 { get; set; }
    }
}
