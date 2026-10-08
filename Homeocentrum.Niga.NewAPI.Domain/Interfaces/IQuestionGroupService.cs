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

        #region Old API compatible overloads
#nullable disable
        /// <summary>
        /// Interface is used to save Question Group
        /// </summary>
        /// <param name="questiongroupModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveQuestionGroup(QuestionGroupModel questiongroupModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Question Group.
        /// </summary>
        /// <param name="questiongroupModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteQuestionGroup(QuestionGroupModel questiongroupModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to GetQuestionGroupByExistanceId .
        /// </summary>
        /// <param name="QuestionSectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<QuestionGroupModel1> GetQuestionGroupByExistanceId(long QuestionSectionId, ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }

}
