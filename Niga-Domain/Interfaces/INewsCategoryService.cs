using System;
using System.Collections.Generic;
using System.Text;
using Niga_Domain.DTOs;

namespace Niga_Domain.Business.Interface
{
    /// <summary>
    /// Interface used for newscategory related operations
    /// </summary>
    public interface INewsCategoryService
    {

        /// <summary>
        /// Method is used for to get newscategory by newscategoryId
        /// </summary>
        /// <param name="newscategoryId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        NewsCategoryModel GetNewsCategoryById(long newscategoryId, ref ErrorResponseModel errorResponseModel);

            /// <summary>
            /// interface for getting all the newscategory
            /// </summary>
            /// <param name="errorResponseModel"></param>
            /// <returns></returns>
        List<NewsCategoryModel> GetAllNewsCategory(ref ErrorResponseModel errorResponseModel);

            /// <summary>
            /// Interface is used to save newscategory
            /// </summary>
            /// <param name="model"></param>
            /// <param name="errorResponseModel"></param>
            /// <returns></returns>
         string SaveNewsCategory(NewsCategoryModel model, ref ErrorResponseModel errorResponseModel);

            /// <summary>
            /// Interface is used to deactivate newscategory.
            /// </summary>
            /// <param name="newscategoryId"></param>
            /// <param name="errorResponseModel"></param>
            /// <returns></returns>
        string DeleteNewsCategory(long newscategoryId, ref ErrorResponseModel errorResponseModel);

    }
}
