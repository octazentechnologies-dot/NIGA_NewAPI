// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Niga_Domain.Business.Interface;
// using Niga_Domain.Model;
// using System;

// namespace Niga_Domain.API.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class NewsCategoryController : BaseAPIController
//     {
//         INewsCategoryService newsCategoryService;

//         /// <summary>
//         /// Used to initialize controller and inject news category service
//         /// </summary>
//         /// <param name="_newsCategoryService"></param>
//         public NewsCategoryController(INewsCategoryService _newsCategoryService) 
//         {
//             newsCategoryService = _newsCategoryService;            
//         }



//         /// <summary>
//         /// To get newscategory by newscategoryId 
//         /// </summary>
//         /// <param name="newscategoryId"></param>
//         /// <returns></returns>
//         [HttpGet("GetNewsCategoryById/{newscategoryId}")]
//         [ProducesResponseType(typeof(NewsCategoryModel), 200)]
//         [ProducesResponseType(typeof(string), 404)]
//         [ProducesResponseType(typeof(string), 400)]
//         [ProducesResponseType(typeof(string), 500)]
//         public IActionResult GetNewsCategoryById(long newscategoryId)
//         {
//             ErrorResponseModel errorResponseModel = null;
//             try
//             {
//                 var newsdetailModel = newsCategoryService.GetNewsCategoryById(newscategoryId, ref errorResponseModel);

//                 if (newsdetailModel != null)
//                 {
//                     return Ok(newsdetailModel);
//                 }
//                 return ReturnErrorResponse(errorResponseModel);
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//             }
//         }



//         /// <summary>
//         /// To Get all NewsDetails
//         /// </summary>
//         /// <returns></returns>
//         [HttpGet("GetAllNewsCategory")]
//         [ProducesResponseType(typeof(NewsCategoryModel), 200)]
//         [ProducesResponseType(typeof(string), 404)]
//         [ProducesResponseType(typeof(string), 400)]
//         [ProducesResponseType(typeof(string), 500)]
//         public IActionResult GetAllNewsCategory()
//         {
//             ErrorResponseModel errorResponseModel = null;
//             try
//             {
//                 var newsModelList = newsCategoryService.GetAllNewsCategory(ref errorResponseModel);

//                 if (newsModelList != null)
//                 {
//                     return Ok(newsModelList);
//                 }
//                 return ReturnErrorResponse(errorResponseModel);
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//             }
//         }

//         /// <summary>
//         /// To add new newsdetails 
//         /// </summary>
//         /// <param name=""></param>
//         /// <returns></returns>
//         [HttpPost("SaveNewsCategory")]
//         [ProducesResponseType(typeof(NewsCategoryModel), 200)]
//         [ProducesResponseType(typeof(string), 404)]
//         [ProducesResponseType(typeof(string), 400)]
//         [ProducesResponseType(typeof(string), 500)]
//         public IActionResult SaveNewsCategory(NewsCategoryModel model)
//         {
//             ErrorResponseModel errorResponseModel = null;
//             try
//             {
//                 var newsModel = newsCategoryService.SaveNewsCategory(model, ref errorResponseModel);

//                 if (newsModel != null)
//                 {
//                     return Ok(newsModel);
//                 }
//                 return ReturnErrorResponse(errorResponseModel);
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//             }
//         }


//         /// <summary>
//         /// To delete newsdetails 
//         /// </summary>
//         /// <param name=""></param>
//         /// <returns></returns>
//         [HttpPost]
//         [Route("DeleteNewsCategory")]
//         [ProducesResponseType(typeof(NewsCategoryModel), 200)]
//         [ProducesResponseType(typeof(string), 404)]
//         [ProducesResponseType(typeof(string), 400)]
//         [ProducesResponseType(typeof(string), 500)]
//         public IActionResult DeleteNewsCategory(int newscategoryId)
//         {
//             ErrorResponseModel errorResponseModel = null;
//             try
//             {
//                 var newsModel = newsCategoryService.DeleteNewsCategory(newscategoryId, ref errorResponseModel);

//                 if (newsModel != null)
//                 {
//                     return Ok(newsModel);
//                 }
//                 return ReturnErrorResponse(errorResponseModel);
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
//             }
//         }

//     }
// }
