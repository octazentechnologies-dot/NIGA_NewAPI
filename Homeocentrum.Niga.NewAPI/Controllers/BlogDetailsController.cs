using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/blog")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogDetailService _blogService;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public BlogController(IBlogDetailService blogService, IMapper mapper, IWebHostEnvironment env)
        {
            _blogService = blogService;
            _mapper = mapper;
            _env = env;
        }

        /// <summary>
        /// To get blog by Blog ID
        /// </summary>
        /// <param name="blogId"></param>
        /// <returns></returns>
        [HttpGet("GetBlogDetailsById/{blogId}")]
        public async Task<object> GetBlogById(long blogId)
        {
            try
            {
                var blogModel = await _blogService.GetBlogDetailsById(blogId);

                if (blogModel != null)
                {
                    // Get base URL (e.g., https://yourdomain.com)
                    var request = HttpContext.Request;
                    var baseUrl = $"{request.Scheme}://{request.Host}";

                    // Update image URLs if needed
                    if (!string.IsNullOrEmpty(blogModel.BlogImage1))
                    {
                        blogModel.BlogImage1 = $"{baseUrl}/Data/{blogModel.BlogImage1}".Replace("\\", "/");
                    }

                    if (!string.IsNullOrEmpty(blogModel.BlogImage2))
                    {
                        blogModel.BlogImage2 = $"{baseUrl}/Data/{blogModel.BlogImage2}".Replace("\\", "/");
                    }

                    return new { Status = 200, Data = blogModel };
                }
                else
                {
                    return new { Status = 400, Data = "No Data Found" };
                }
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// To get all Blogs
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetBlogList")]
        public async Task<List<BlogDetailModel1>> ShowBlogList(
            [FromQuery] ParameterParams parameterParams
        )
        {
            var blogList = await _blogService.getAllBlogs(parameterParams);
            Response.AddPaginationHeader(
                blogList.CurrentPage,
                blogList.PageSize,
                blogList.TotalCount,
                blogList.TotalPages
            );
            return blogList;
        }

        [HttpPost("AddBlog")]
        public async Task<object> AddNewBlog(BlogDetailModel1 blogDetail)
        {
            try
            {
                if (!string.IsNullOrEmpty(blogDetail.Src1))
                    blogDetail.BlogImage1 = await SaveBase64Image(blogDetail.Src1, UploadedMedia.Blogs);
                if (!string.IsNullOrEmpty(blogDetail.Src2))
                    blogDetail.BlogImage2 = await SaveBase64Image(blogDetail.Src2, UploadedMedia.Blogs);
                var Blog = _mapper.Map<BlogDetail>(blogDetail);
                _blogService.SaveBlog(Blog);
                if (await _blogService.SaveAllAsync())
                {
                    return new { Status = 200, Meassage = "Data Added Successfully" };
                }
                else
                {
                    return new { Status = 400, Meassage = "Failed To Add Data" };
                }
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        private async Task<string> SaveBase64Image(string base64String, string folderName)
        {
            var base64Parts = base64String.Split(',');
            var imageData = base64Parts.Length > 1 ? base64Parts[1] : base64Parts[0];
            var imageBytes = Convert.FromBase64String(imageData);

            var uploadsFolder = UploadedMedia.Folder(_env.ContentRootPath, folderName);
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + ".png";
            await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadsFolder, fileName), imageBytes);

            return UploadedMedia.MediaRelative(folderName, fileName);
        }

        [HttpPost("UpdateBlogDetails")]
        public async Task<IActionResult> UpdateBlogDetails(
            [FromBody] BlogDetailModel1 updateBlogDto
        )
        {
            try
            {
                var existingBlog = await _blogService.GetBlogById(updateBlogDto.BlogId);
                if (existingBlog == null)
                {
                    return NotFound(new { Status = 404, Message = "Blog not found" });
                }

                // Handle first image (Src1)
                if (
                    !string.IsNullOrEmpty(updateBlogDto.Src1)
                    && updateBlogDto.Src1.StartsWith("data:image")
                )
                {
                    updateBlogDto.BlogImage1 = await SaveBase64Image(updateBlogDto.Src1, "Blogs");
                }

                // Handle second image (Src2)
                if (
                    !string.IsNullOrEmpty(updateBlogDto.Src2)
                    && updateBlogDto.Src2.StartsWith("data:image")
                )
                {
                    updateBlogDto.BlogImage2 = await SaveBase64Image(updateBlogDto.Src2, "Blogs");
                }

                // Map the updated fields into the existing entity
                _mapper.Map(updateBlogDto, existingBlog);

                _blogService.UpdateBlog(existingBlog);

                if (await _blogService.SaveAllAsync())
                {
                    return Ok(new { Status = 200, Message = "Data Updated Successfully" });
                }
                else
                {
                    return BadRequest(new { Status = 400, Message = "Failed To Update Data" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpPost("DeleteBlogDetails/{Id}")]
        public async Task<object> DeleteBlogDetails(int Id)
        {
            var data = await _blogService.GetBlogById(Id);
            try
            {
                _blogService.DeleteBlog(data);
                if (await _blogService.SaveAllAsync())
                {
                    return new { Status = 200, Meassage = "Data Deleted Successfully" };
                }
                else
                {
                    return new { Status = 400, Meassage = "Failed To Deleted Data" };
                }
            }
            catch (Exception ex)
            {
                var result = new { Status = 500, Message = ex.Message };
                return BadRequest(result);
            }
        }
    }
}
