using API.Helpers;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using System;
using System.Net;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
{
    public class BlogService : IBlogDetailService
    {
        private readonly NIGACentrumContext _context;
        public BlogService(NIGACentrumContext context)
        {
            _context = context;
        }
        public async Task<BlogDetail> GetBlogById(long blogId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var blogEntity = await _context.BlogDetails.FirstOrDefaultAsync(x => x.BlogId == blogId && x.IsActive==true);
            if (blogEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Blog not found";
            }
            return blogEntity;
        }

        public async Task<PagedList<BlogDetailModel1>> getAllBlogs(ParameterParams parameter)
        {
            var blogModelQuery = (from x in _context.BlogDetails
                                  select new BlogDetailModel1
                                  {
                                      BlogId = x.BlogId,
                                      BlogHead = x.BlogHead,
                                      BlogDate = x.BlogDate,
                                      BlogSubHead = x.BlogSubHead,
                                      BlogDetails1 = x.BlogDetails,
                                      BlogImage1 = x.BlogImage1,
                                      BlogImage2 = x.BlogImage2,
                                      IsActive = x.IsActive,
                                  }).AsQueryable();
            if (!string.IsNullOrEmpty(parameter.search))
            {
                blogModelQuery = blogModelQuery.Where(x => x.BlogHead.ToLower().Contains(parameter.search.ToLower()));

            }
            return await PagedList<BlogDetailModel1>.CreateAsync(blogModelQuery.AsNoTracking(), parameter.PageNumber,
                parameter.PageSize);

        }


        public void SaveBlog(BlogDetail blog)
        {
            _context.Entry(blog).State = EntityState.Added;
        }

        public void UpdateBlog(BlogDetail blog)
        {
            _context.Entry(blog).State = EntityState.Modified;

        }

        public void DeleteBlog(BlogDetail blog)
        {
            blog.IsActive = false;
            _context.Entry(blog).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<BlogDetailModel1> GetBlogDetailsById(long blogId)
        {
            var blogDetails = await (from s in _context.BlogDetails
                                     where s.BlogId == blogId && s.IsActive == true
                                     select new BlogDetailModel1
                                     {

                                         BlogId = s.BlogId,
                                         BlogHead = s.BlogHead,
                                         BlogDate = s.BlogDate,
                                         BlogSubHead = s.BlogSubHead,
                                         BlogDetails1 = s.BlogDetails,
                                         BlogImage1 = s.BlogImage1,
                                         BlogImage2 = s.BlogImage2,
                                         IsActive = s.IsActive,
                                     })
                                     .FirstOrDefaultAsync();
            return blogDetails;
        }


        #region Old API compatible methods
#nullable disable

        /// <summary>
        /// Interface is used to deactivate Blogdetail.
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteBlogDetail(long blogId, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var blogEntity = _context.BlogDetails.FirstOrDefault(x => x.BlogId == blogId);
            if (blogEntity != null)
            {
                blogEntity.IsActive=false;
                _context.SaveChanges();
                Message = "BlogDetail Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// interface for getting all the Blogdetail
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<BlogDetailModel> GetAllBlogDetail(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var blogList=new List<BlogDetailModel>();
            var blogEntity=_context.BlogDetails.Where(x=>x.IsActive==true).ToList();
            if(blogEntity.Count==0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Blog Details not found";
            }
            var audit = LoadBlogAuditUsers(blogEntity.Select(b => b.BlogId).ToList());
            blogEntity.ForEach(item =>
            {
                blogList.Add(new BlogDetailModel
                {
                    BlogId= item.BlogId,
                    BlogHead= item.BlogHead,
                    BlogSubHead= item.BlogSubHead,
                    //BlogDate= item.BlogDate,
                   BlogDate= item.BlogDate.HasValue ? item.BlogDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                    BlogImage1= item.BlogImage1,
                    BlogImage2=  item.BlogImage2,
                    BlogDetails1 = item.BlogDetails,
                    IsActive=item.IsActive,
                    EnteredBy=audit.TryGetValue(item.BlogId, out var a) ? a.EnteredBy : null,
                    EnteredDate=item.EnteredDate ?? default,
                    ChangedBy=audit.TryGetValue(item.BlogId, out var c) ? c.ChangedBy : null,
                    ChangedDate=item.ChangedDate,
                });
            });
            return blogList;
        }

        /// <summary>
        /// Method is used for to get Blogdetail by blogId
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public BlogDetailModel1 GetBlogDetailById(long blogId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var blogEntity = _context.BlogDetails.Where(x => x.BlogId == blogId).FirstOrDefault();
            if(blogEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Blog Details not found";
                return null;
            }
            var audit = LoadBlogAuditUsers(new List<int> { blogEntity.BlogId });
            audit.TryGetValue(blogEntity.BlogId, out var users);
            return new BlogDetailModel1
            {
                BlogId = blogEntity.BlogId,
                BlogHead = blogEntity.BlogHead,
                BlogSubHead = blogEntity.BlogSubHead,
                BlogDate = blogEntity.BlogDate,
             //   BlogDate = blogEntity.BlogDate.HasValue ? blogEntity.BlogDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                BlogImage1 = blogEntity.BlogImage1,
                BlogImage2 =  blogEntity.BlogImage2,
                BlogDetails1 = blogEntity.BlogDetails,
                IsActive = blogEntity.IsActive,
                EnteredBy = users.EnteredBy,
                EnteredDate = blogEntity.EnteredDate ?? default,
                ChangedBy = users.ChangedBy,
                ChangedDate = blogEntity.ChangedDate,
            };
        }

        /// <summary>
        /// Interface is used to save/update Blogdetail
        /// </summary>
        /// <param name="model"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveBlogDetail(BlogDetailModel1 model, ref ErrorResponseModel errorResponseModel)
        {
            string message = "";
            if (model.BlogId == 0)
            {
                BlogDetail details = new BlogDetail();
                details.BlogId = model.BlogId;
                details.BlogHead = model.BlogHead;
                details.BlogSubHead = model.BlogSubHead;
                details.BlogDate = model.BlogDate;
                details.BlogImage1 = model.BlogImage1;
                details.BlogImage2 = model.BlogImage2;
                details.BlogDetails = model.BlogDetails1;
                details.EnteredDate = DateTime.Now;
                details.IsActive = true;
                _context.BlogDetails.Add(details);
                _context.SaveChanges();
                if (model.EnteredBy.HasValue)
                {
                    _context.Database.ExecuteSqlInterpolated(
                        $"UPDATE dbo.BlogDetails SET EnteredBy = {model.EnteredBy.Value} WHERE BlogId = {details.BlogId}");
                }
                message = "Blog Details saved Successfully";
            }
            else
            {
                var details = _context.BlogDetails.FirstOrDefault(x => x.BlogId == model.BlogId);
                if (details != null)
                {
                    details.BlogId = model.BlogId;
                    details.BlogHead = model.BlogHead;
                    details.BlogSubHead = model.BlogSubHead;
                    details.BlogDate = model.BlogDate;
                    details.BlogImage1 = model.BlogImage1;
                    details.BlogImage2 = model.BlogImage2;
                    details.BlogDetails = model.BlogDetails1;
                  //  details.ChangedBy = model.ChangedBy;
                    details.ChangedDate = DateTime.Now;
                    details.IsActive =true;
                    _context.SaveChanges();
                    message = "Blog Details Update Successfully";
                }
            }
            return message;
        }

        private Dictionary<int, (int? EnteredBy, int? ChangedBy)> LoadBlogAuditUsers(List<int> blogIds)
        {
            if (blogIds.Count == 0)
                return new Dictionary<int, (int? EnteredBy, int? ChangedBy)>();
            return _context.Database
                .SqlQueryRaw<BlogAuditRow>(
                    "SELECT BlogId, EnteredBy, ChangedBy FROM dbo.BlogDetails WHERE BlogId IN (SELECT CAST(value AS int) FROM STRING_SPLIT({0}, ','))",
                    string.Join(",", blogIds))
                .ToList()
                .ToDictionary(r => r.BlogId, r => (r.EnteredBy, r.ChangedBy));
        }

        private sealed class BlogAuditRow
        {
            public int BlogId { get; set; }
            public int? EnteredBy { get; set; }
            public int? ChangedBy { get; set; }
        }

#nullable restore
        #endregion
    }
}
