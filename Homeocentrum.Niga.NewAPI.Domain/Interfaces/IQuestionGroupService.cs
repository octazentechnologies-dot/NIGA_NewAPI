using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
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
