using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
{
    /// <summary>
    /// Interface used for qualification related operations
    /// </summary>
    public interface IQualificationService
    {
        /// <summary>
        /// Method is used for to get qualification by qualificationId
        /// </summary>
        /// <param name="qualificationId"></param>
        /// <returns></returns>
        Task<QualificationMaster> GetQualificationById(long qualificationId);

        /// <summary>
        /// Method is used for get all the Qualifications
        /// </summary>
        /// <param name="parameterParams"></param>
        /// <returns></returns>
        Task<PagedList<QualificationModel>> GetAllQualifications(ParameterParams parameterParams);

        /// <summary>
        /// Method is used for get all the Qualifications by filter
        /// </summary>
        /// <param name="search"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<QualificationModel> GetAllQualificationsByFilter(string search, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to save Qualification
        /// </summary>
        /// <param name="qualification"></param>
        void SaveQualification(QualificationMaster qualification);

        /// <summary>
        /// Interface is used to update Qualification
        /// </summary>
        /// <param name="qualification"></param>
        void UpdateQualification(QualificationMaster qualification);

        /// <summary>
        /// Interface is used to deactivate Qualification.
        /// </summary>
        /// <param name="qualification"></param>
        void DeleteQualification(QualificationMaster qualification);

        /// <summary>
        /// Method is used for get qualification details by qualificationId
        /// </summary>
        /// <param name="qualificationId"></param>
        /// <returns></returns>
        Task<QualificationModel> GetQualificationDetailsById(long qualificationId);

        /// <summary>
        /// Method is used for save all qualifications
        /// </summary>
        /// <returns></returns>
        Task<bool> SaveAllAsync();

        /// <summary>
        /// Method is used for get qualification dropdown
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
    }
}
