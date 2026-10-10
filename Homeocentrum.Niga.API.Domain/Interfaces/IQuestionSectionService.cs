using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Helpers;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Interfaces
{
    /// <summary>
    /// Interface used for question section related operations
    /// </summary>
    public interface IQuestionSectionService
    {
        /// <summary>
        /// Method is used for to get question section by questionSectionId
        /// </summary>
        /// <param name="questionSectionId"></param>
        /// <returns></returns>
        Task<QuestionSectionMaster> GetQuestionSectionById(long questionSectionId);

        /// <summary>
        /// Method is used for get all the Question Sections
        /// </summary>
        /// <param name="parameterParams"></param>
        /// <returns></returns>
        Task<PagedList<QuestionSectionModel>> GetAllQuestionSections(ParameterParams parameterParams);

        /// <summary>
        /// Interface is used to save Question Section
        /// </summary>
        /// <param name="questionSection"></param>
        void SaveQuestionSection(QuestionSectionMaster questionSection);

        /// <summary>
        /// Interface is used to update Question Section
        /// </summary>
        /// <param name="questionSection"></param>
        void UpdateQuestionSection(QuestionSectionMaster questionSection);

        /// <summary>
        /// Interface is used to deactivate Question Section
        /// </summary>
        /// <param name="questionSection"></param>
        void DeleteQuestionSection(QuestionSectionMaster questionSection);

        /// <summary>
        /// Method is used for get question section details by questionSectionId
        /// </summary>
        /// <param name="questionSectionId"></param>
        /// <returns></returns>
        Task<QuestionSectionModel> GetQuestionSectionDetailsById(long questionSectionId);

        /// <summary>
        /// Method is used for save all question sections
        /// </summary>
        /// <returns></returns>
        Task<bool> SaveAllAsync();

        /// <summary>
        /// Method is used for get question section dropdown
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
         Task<List<QuestionSectionModel>> GetQuestionSectionDD(string? search);

        #region Old API compatible overloads
#nullable disable
        /// <summary>
        /// Interface is used to save Question Section
        /// </summary>
        /// <param name="questionSectionModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveQuestionSection(QuestionSectionModel questionSectionModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Question Section.
        /// </summary>
        /// <param name="questionSectionModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteQuestionSection(QuestionSectionModel questionSectionModel, ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }
}

