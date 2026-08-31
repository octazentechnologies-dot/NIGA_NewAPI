// using API.Helpers;
// using Microsoft.AspNetCore.Mvc;
// using Niga_Domain.Business.Interface;
// using Niga_Domain.DTOs;
// using Niga_Domain.Helpers;
// using Niga_Domain.Master;
// using System;
// using System.IO;
// using System.Threading.Tasks;

// namespace Niga_Domain.API.Controllers
// {
//     [Route("api/news")]
//     [ApiController]
//     public class NewsDetailController : ControllerBase
//     {
//         private readonly INewsDetailService _newsService;

//         public NewsDetailController(INewsDetailService newsService)
//         {
//             _newsService = newsService;
//         }

//         [HttpGet("GetNewsDetailsById/{newsId}")]
//         public async Task<object> GetNewsById(long newsId)
//         {
//             var newsModel = await _newsService.GetNewsDetailsById(newsId);
//             if (newsModel != null)
//             {
//                 var request = HttpContext.Request;
//                 var baseUrl = $"{request.Scheme}://{request.Host}";
//                 if (!string.IsNullOrEmpty(newsModel.NewsImage1))
//                     newsModel.NewsImage1 = $"{baseUrl}/Data/{newsModel.NewsImage1}".Replace("\\", "/");
//                 if (!string.IsNullOrEmpty(newsModel.NewsImage2))
//                     newsModel.NewsImage2 = $"{baseUrl}/Data/{newsModel.NewsImage2}".Replace("\\", "/");
//                 if (!string.IsNullOrEmpty(newsModel.NewsImage3))
//                     newsModel.NewsImage3 = $"{baseUrl}/Data/{newsModel.NewsImage3}".Replace("\\", "/");
//                 if (!string.IsNullOrEmpty(newsModel.NewsImage4))
//                     newsModel.NewsImage4 = $"{baseUrl}/Data/{newsModel.NewsImage4}".Replace("\\", "/");
//                 return new { Status = 200, Data = newsModel };
//             }
//             else
//             {
//                 return new { Status = 400, Data = "No Data Found" };
//             }
//         }

//         [HttpGet("GetNewsList")]
//         public async Task<PagedList<NewDetailModel1>> ShowNewsList([FromQuery] ParameterParams parameterParams)
//         {
//             return await _newsService.GetAllNews(parameterParams);
//         }

//         [HttpPost("AddNews")]
//         public async Task<object> AddNewNews(NewDetailModel1 newsDetail)
//         {
//             try
//             {
//                 // Handle base64 images
//                 if (!string.IsNullOrEmpty(newsDetail.NewsImage1) && newsDetail.NewsImage1.StartsWith("data:image"))
//                     newsDetail.NewsImage1 = await _newsService.SaveBase64Image(newsDetail.NewsImage1, "News");
//                 if (!string.IsNullOrEmpty(newsDetail.NewsImage2) && newsDetail.NewsImage2.StartsWith("data:image"))
//                     newsDetail.NewsImage2 = await _newsService.SaveBase64Image(newsDetail.NewsImage2, "News");
//                 if (!string.IsNullOrEmpty(newsDetail.NewsImage3) && newsDetail.NewsImage3.StartsWith("data:image"))
//                     newsDetail.NewsImage3 = await _newsService.SaveBase64Image(newsDetail.NewsImage3, "News");
//                 if (!string.IsNullOrEmpty(newsDetail.NewsImage4) && newsDetail.NewsImage4.StartsWith("data:image"))
//                     newsDetail.NewsImage4 = await _newsService.SaveBase64Image(newsDetail.NewsImage4, "News");

//                 var news = new NewsDetail();
//                 // Map properties manually or use AutoMapper if available
//                 news.NewsHeading = newsDetail.NewsHeading;
//                 news.NewsSubHeading = newsDetail.NewsSubHeading;
//                 news.NewsDate = newsDetail.NewsDate;
//                 news.NewsImage1 = newsDetail.NewsImage1;
//                 news.NewsImage2 = newsDetail.NewsImage2;
//                 news.NewsImage3 = newsDetail.NewsImage3;
//                 news.NewsImage4 = newsDetail.NewsImage4;
//                 news.NewsContent = newsDetail.NewsContent;
//                 news.NewsCategoryId = newsDetail.NewsCategoryId;
//                 news.IsActive = true;

//                 _newsService.SaveNews(news);
//                 if (await _newsService.SaveAllAsync())
//                 {
//                     return new { Status = 200, Message = "Data Added Successfully" };
//                 }
//                 else
//                 {
//                     return new { Status = 400, Message = "Failed To Add Data" };
//                 }
//             }
//             catch (Exception ex)
//             {
//                 return new { Status = 500, Message = ex.Message };
//             }
//         }

//         [HttpPost("UpdateNews")]
//         public async Task<IActionResult> UpdateNews([FromBody] NewDetailModel1 updateNewsDto)
//         {
//             var existingNews = await _newsService.GetNewsById(updateNewsDto.NewsId);
//             if (existingNews == null)
//                 return NotFound(new { Status = 404, Message = "News not found" });

//             if (!string.IsNullOrEmpty(updateNewsDto.NewsImage1) && updateNewsDto.NewsImage1.StartsWith("data:image"))
//                 updateNewsDto.NewsImage1 = await _newsService.SaveBase64Image(updateNewsDto.NewsImage1, "News");
//             if (!string.IsNullOrEmpty(updateNewsDto.NewsImage2) && updateNewsDto.NewsImage2.StartsWith("data:image"))
//                 updateNewsDto.NewsImage2 = await _newsService.SaveBase64Image(updateNewsDto.NewsImage2, "News");
//             if (!string.IsNullOrEmpty(updateNewsDto.NewsImage3) && updateNewsDto.NewsImage3.StartsWith("data:image"))
//                 updateNewsDto.NewsImage3 = await _newsService.SaveBase64Image(updateNewsDto.NewsImage3, "News");
//             if (!string.IsNullOrEmpty(updateNewsDto.NewsImage4) && updateNewsDto.NewsImage4.StartsWith("data:image"))
//                 updateNewsDto.NewsImage4 = await _newsService.SaveBase64Image(updateNewsDto.NewsImage4, "News");

//             // Map updated fields
//             existingNews.NewsHeading = updateNewsDto.NewsHeading;
//             existingNews.NewsSubHeading = updateNewsDto.NewsSubHeading;
//             existingNews.NewsDate = updateNewsDto.NewsDate;
//             existingNews.NewsImage1 = updateNewsDto.NewsImage1;
//             existingNews.NewsImage2 = updateNewsDto.NewsImage2;
//             existingNews.NewsImage3 = updateNewsDto.NewsImage3;
//             existingNews.NewsImage4 = updateNewsDto.NewsImage4;
//             existingNews.NewsContent = updateNewsDto.NewsContent;
//             existingNews.NewsCategoryId = updateNewsDto.NewsCategoryId;

//             _newsService.UpdateNews(existingNews);

//             if (await _newsService.SaveAllAsync())
//                 return Ok(new { Status = 200, Message = "Data Updated Successfully" });
//             else
//                 return BadRequest(new { Status = 400, Message = "Failed To Update Data" });
//         }

//         [HttpPost("DeleteNews/{id}")]
//         public async Task<object> DeleteNews(int id)
//         {
//             var data = await _newsService.GetNewsById(id);
//             try
//             {
//                 _newsService.DeleteNews(data);
//                 if (await _newsService.SaveAllAsync())
//                 {
//                     return new { Status = 200, Message = "Data Deleted Successfully" };
//                 }
//                 else
//                 {
//                     return new { Status = 400, Message = "Failed To Delete Data" };
//                 }
//             }
//             catch (Exception ex)
//             {
//                 return new { Status = 500, Message = ex.Message };
//             }
//         }
//     }
// }