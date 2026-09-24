using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    /// <summary>
    /// Interface used for news details related operations
    /// </summary>
    public interface INewsDetailService
    {
        /// <summary>
        /// Method is used for to get newsdetails by newsId
        /// </summary>
        /// <param name="newsId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        NewDetailModel1 GetNewsDetailsbyId(long newsId, ref ErrorResponseModel errorResponseModel);




        /// <summary>
        /// interface for getting all the newsdetails
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<NewDetailModel> GetAllNewsDetails(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to save newsdetails
        /// </summary>
        /// <param name="model"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveNewsDetails(NewDetailModel1 model, ref ErrorResponseModel errorResponseModel);


        /// <summary>
                /// Interface is used to deactivate newsdetails.
                /// </summary>
                /// <param name="newsId"></param>
                /// <param name="errorResponseModel"></param>
                /// <returns></returns>
        string DeleteNewsDetails(int newsId, ref ErrorResponseModel errorResponseModel);


        /// <summary>
        /// Method is used for to get newsdetails by newsId
        /// </summary>
        /// <param name="newsCategoryId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
       List<NewDetailModel> GetNewsDetailsbyCategoryId(long newsCategoryId, ref ErrorResponseModel errorResponseModel);

    }
}
