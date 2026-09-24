using API.Helpers;
using AutoMapper;
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
        private readonly IMapper _mapper;

        public BlogService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
                                     })
                                     .FirstOrDefaultAsync();
            return blogDetails;
        }


    }
}
