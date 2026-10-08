using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    /// <summary>
    /// Interface used for Blogdetail related operations
    /// </summary>
    public interface IBlogDetailService
    {
      Task<BlogDetail> GetBlogById(long blogId);
        Task<PagedList<BlogDetailModel1>> getAllBlogs(ParameterParams parameterParams);
        void SaveBlog(BlogDetail blog);
        void UpdateBlog(BlogDetail blog);
        void DeleteBlog(BlogDetail blog);
        Task<BlogDetailModel1> GetBlogDetailsById(long blogId);
        Task<bool> SaveAllAsync();

        #region Old API compatible methods
#nullable disable
        /// <summary>
        /// Method is used for to get Blogdetail by blogId
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        BlogDetailModel1 GetBlogDetailById(long blogId, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the Blogdetail
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<BlogDetailModel> GetAllBlogDetail(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save/update Blogdetail
        /// </summary>
        /// <param name="model"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveBlogDetail(BlogDetailModel1 model, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Blogdetail.
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteBlogDetail(long blogId, ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }
}
