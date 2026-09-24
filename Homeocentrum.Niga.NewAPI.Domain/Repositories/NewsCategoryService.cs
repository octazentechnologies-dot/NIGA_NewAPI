// using Microsoft.Extensions.Options;
// using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
// using Homeocentrum.Niga.NewAPI.Domain.Data;
// using Homeocentrum.Niga.NewAPI.Domain.DTOs;
// using System.Net;
// using System.Text;

// namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
// {

//     public class NewsCategoryService : INewsCategoryService
//     {
//         NIGACentrumContext context;

//         /// <summary>
//         /// Creating constructor and injection dbContext
//         /// </summary>
//         /// <param name="centrumContext"></param>
//         public NewsCategoryService(NIGACentrumContext centrumContext)
//         {
//             context = centrumContext;

//         }
//         public string DeleteNewsCategory(long newscategoryId, ref ErrorResponseModel errorResponseModel)
//         {
//             string Message = "";
//             var newsEntity = context.NewsCategory.FirstOrDefault(x => x.NewsCategoryId == newscategoryId);
//             if (newsEntity != null)
//             {
//                 newsEntity.IsActive = false;
//                 context.SaveChanges();
//                 Message = " News Category Delete Successfully";
//             }
//             return Message;
//         }

//         public List<NewsCategoryModel> GetAllNewsCategory(ref ErrorResponseModel errorResponseModel)
//         {
//             errorResponseModel = new ErrorResponseModel();
//             var newscategoryList=new List<NewsCategoryModel>();
//             var categoryEntity=context.NewsCategories.Where(x=>x.IsActive==true).ToList();
//             if(categoryEntity == null)
//             {
//                 errorResponseModel.StatusCode = HttpStatusCode.NotFound;
//                 errorResponseModel.Message = "News Category not found";
//             }
//             categoryEntity.ForEach(item =>
//             {
//                 newscategoryList.Add(new NewsCategoryModel
//                 {
//                     NewsCategoryId = item.NewsCategoryId,
//                     NewsCategory1=item.NewsCategory1,
//                     SeqNo = item.SeqNo,
                    
//                 });
//             });
//             return newscategoryList;
//         }

//         public NewsCategoryModel GetNewsCategoryById(long newscategoryId, ref ErrorResponseModel errorResponseModel)
//         {
//             errorResponseModel= new ErrorResponseModel();
//             var newsEntity = context.NewsCategory.Where(x => x.NewsCategoryId == newscategoryId).FirstOrDefault();
//             if(newsEntity == null)
//             {
//                 errorResponseModel.StatusCode = HttpStatusCode.NotFound;
//                 errorResponseModel.Message = "News Category not found";
//             }
//             return new NewsCategoryModel
//             {
//                 NewsCategoryId=newsEntity.NewsCategoryId,
//                 NewsCategory1=newsEntity.NewsCategory1,
//                 SeqNo=newsEntity.SeqNo,
//                 IsActive=newsEntity.IsActive
//             };
//         }

//         public string SaveNewsCategory(NewsCategoryModel model, ref ErrorResponseModel errorResponseModel)
//         {
//             string message = "";
//             if (model.NewsCategoryId == 0)
//             {
//                 NewsCategory category = new NewsCategory();
//                 category.NewsCategoryId = model.NewsCategoryId;
//                 category.NewsCategory1 = model.NewsCategory1;
//                 category.SeqNo = model.SeqNo;
//                 category.IsActive = true;
//                 context.NewsCategory.Add(category);
//                 context.SaveChanges();
//                 message = "News Category saved Successfully";
//             }
//             else
//             {
//                 var category = context.NewsCategory.FirstOrDefault(x => x.NewsCategoryId == model.NewsCategoryId);
//                 if (category != null)
//                 {
//                     category.NewsCategoryId=model.NewsCategoryId;
//                     category.NewsCategory1=model.NewsCategory1;
//                     category.SeqNo = model.SeqNo;
//                     category.IsActive = true;
//                     context.SaveChanges ();
//                     message = "News Category Update Successfully";
//                 }
                    
//             }
//             return message;
//         }
//     }
// }
