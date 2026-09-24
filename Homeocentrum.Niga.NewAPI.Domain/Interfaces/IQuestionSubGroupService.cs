using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    /// <summary>
    /// Interface used for question sub group related operations
    /// </summary>
    public interface IQuestionSubGroupService
    {
        /// <summary>
        /// Method is used for to get question sub group by questionSubGroupId
        /// </summary>
        /// <param name="questionSubGroupId"></param>
        /// <returns></returns>
        Task<QuestionSubgroup> GetQuestionSubGroupById(long questionSubGroupId);

        /// <summary>
        /// Method is used for get all the Question Sub Groups
        /// </summary>
        /// <param name="parameterParams"></param>
        /// <returns></returns>
        Task<PagedList<QuestionSubGroupModel>> GetAllQuestionSubGroups(ParameterParams parameterParams);

        /// <summary>
        /// Method is used for get all the Question Sub Groups by filter
        /// </summary>
        /// <param name="search"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        Task<List<QuestionSubGroupModel>> GetQuestionSubGroupDD(string search);

        /// <summary>
        /// Interface is used to save Question Sub Group
        /// </summary>
        /// <param name="questionSubGroup"></param>
        void SaveQuestionSubGroup(QuestionSubgroup questionSubGroup);

        /// <summary>
        /// Interface is used to update Question Sub Group
        /// </summary>
        /// <param name="questionSubGroup"></param>
        void UpdateQuestionSubGroup(QuestionSubgroup questionSubGroup);

        /// <summary>
        /// Interface is used to deactivate Question Sub Group
        /// </summary>
        /// <param name="questionSubGroup"></param>
        void DeleteQuestionSubGroup(QuestionSubgroup questionSubGroup);

        /// <summary>
        /// Method is used for get question sub group details by questionSubGroupId
        /// </summary>
        /// <param name="questionSubGroupId"></param>
        /// <returns></returns>
        Task<QuestionSubGroupModel> GetQuestionSubGroupDetailsById(long questionSubGroupId);

        /// <summary>
        /// Method is used for save all question sub groups
        /// </summary>
        /// <returns></returns>
        Task<bool> SaveAllAsync();
    }
}
