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

    }
}
