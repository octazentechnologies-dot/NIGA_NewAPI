using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;

namespace Niga_Domain.Business.Interface
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
