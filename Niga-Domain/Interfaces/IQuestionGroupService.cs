using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;

namespace Niga_Domain.Interface
{
    public interface IQuestionGroupService
    {

        Task<QuestionGroupMaster> GetQuestionById(long questiongroupId);

        Task<QuestionGroupModel> GetQuestionDetailsById(long questiongroupId);
        Task<PagedList<QuestionGroupModel1>> GetQuestionGroupList(ParameterParams parameterParams);

        // List<QuestionGroupModel1> GetQuestionGroupExistance(ref ErrorResponseModel errorResponseModel);
        Task<List<QuestionGroupModel1>> GetQuestionGroupByExistanceId(long QuestionSectionId);
        void SaveQuestionGroup(QuestionGroupMaster questionGroup);
        void UpdateQuestionGroup(QuestionGroupMaster questionGroup);
        void DeleteQuestionGroup(QuestionGroupMaster questionGroup);
        Task<bool> SaveAllAsync();

    }

}
