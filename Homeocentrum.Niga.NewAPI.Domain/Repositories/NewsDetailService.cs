using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using API.Helpers;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
{
    public class NewsDetailService : INewsDetailService
    {
        private readonly NIGACentrumContext _context;

        public NewsDetailService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<NewsDetail> GetNewsById(long newsId)
        {
            return await _context.NewsDetails.FirstOrDefaultAsync(x => x.NewsId == newsId && x.IsActive == true);
        }

        public async Task<PagedList<NewDetailModel1>> GetAllNews(ParameterParams parameter)
        {
            var newsQuery = (from x in _context.NewsDetails
                             select new NewDetailModel1
                             {
                                 NewsId = x.NewsId,
                                 NewsHeading = x.NewsHeading,
                                 NewsSubHeading = x.NewsSubHeading,
                                 NewsDate = x.NewsDate,
                                 NewsImage1 = x.NewsImage1,
                                 NewsImage2 = x.NewsImage2,
                                 NewsImage3 = x.NewsImage3,
                                 NewsImage4 = x.NewsImage4,
                                 NewsContent = x.NewsContent,
                                 NewsCategoryId = x.NewsCategoryId,
                                 IsActive = x.IsActive
                             }).AsQueryable();
            if (!string.IsNullOrEmpty(parameter.search))
            {
                newsQuery = newsQuery.Where(x => x.NewsHeading.ToLower().Contains(parameter.search.ToLower()));
            }
            if(parameter.categoryId > 0)
            {
                newsQuery = newsQuery.Where(x => x.NewsCategoryId == parameter.categoryId);
            }
            return await PagedList<NewDetailModel1>.CreateAsync(newsQuery.AsNoTracking(), parameter.PageNumber, parameter.PageSize);
        }

        public void SaveNews(NewsDetail news)
        {
            _context.Entry(news).State = EntityState.Added;
        }

        public void UpdateNews(NewsDetail news)
        {
            _context.Entry(news).State = EntityState.Modified;
        }

        public void DeleteNews(NewsDetail news)
        {
            news.IsActive = false;
            _context.Entry(news).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> SaveBase64Image(string base64String, string folderName)
        {
            var base64Parts = base64String.Split(',');
            var imageData = base64Parts.Length > 1 ? base64Parts[1] : base64Parts[0];
            var imageBytes = Convert.FromBase64String(imageData);
            var uploadsFolder = UploadedMedia.Folder(Directory.GetCurrentDirectory(), folderName);
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + ".png";
            await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadsFolder, fileName), imageBytes);
            return UploadedMedia.MediaRelative(folderName, fileName);
        }

        #region Old API compatible methods
#nullable disable
        public NewDetailModel1 GetNewsDetailsbyId(long newsId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var newsEntity = (from news in _context.NewsDetails
                              join category in _context.NewsCategories
                              on news.NewsCategoryId equals category.NewsCategoryId
                              where news.NewsId == newsId
                              select new
                              {
                                  news.NewsId,
                                  news.NewsCategoryId,
                                  news.NewsDate,
                                  news.NewsHeading,
                                  news.NewsSubHeading,
                                  news.NewsImage1,
                                  news.NewsImage2,
                                  news.NewsImage3,
                                  news.NewsImage4,
                                  news.EnteredBy,
                                  news.EnteredDate,
                                  category.NewsCategory1,
                                  news.NewsContent,
                                  news.IsActive
                              }).AsNoTracking().FirstOrDefault();

            if (newsEntity == null)
            {
                errorResponseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                errorResponseModel.Message = "News Details not found";
                return null;
            }
            return new NewDetailModel1
            {
                NewsId = newsEntity.NewsId,
                NewsDate = newsEntity.NewsDate,
                NewsHeading = newsEntity.NewsHeading,
                NewsSubHeading = newsEntity.NewsSubHeading,
                NewsCategoryId = newsEntity.NewsCategoryId,
                NewsContent = newsEntity.NewsContent,
                NewsImage1 = newsEntity.NewsImage1,
                NewsImage2 = newsEntity.NewsImage2,
                NewsImage3 = newsEntity.NewsImage3,
                NewsImage4 = newsEntity.NewsImage4,
                EnteredBy = newsEntity.EnteredBy,
                EnteredDate = newsEntity.EnteredDate,
                NewsCategory1 = newsEntity.NewsCategory1,
                IsActive = newsEntity.IsActive,
            };
        }

        public List<NewDetailModel> GetAllNewsDetails(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var list = GetNewsDetailList(null);
            if (list.Count == 0)
            {
                errorResponseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                errorResponseModel.Message = "News Details not found";
            }
            return list;
        }

        public string SaveNewsDetails(NewDetailModel1 model, ref ErrorResponseModel errorResponseModel)
        {
            string message = "";
            if (model.NewsId == 0)
            {
                var details = new NewsDetail
                {
                    NewsDate = model.NewsDate,
                    NewsHeading = model.NewsHeading,
                    NewsSubHeading = model.NewsSubHeading,
                    NewsContent = model.NewsContent,
                    NewsImage1 = model.NewsImage1,
                    NewsImage2 = model.NewsImage2,
                    NewsImage3 = model.NewsImage3,
                    NewsImage4 = model.NewsImage4,
                    NewsCategoryId = model.NewsCategoryId,
                    EnteredBy = model.EnteredBy,
                    EnteredDate = DateTime.Now,
                    IsActive = model.IsActive,
                };
                _context.NewsDetails.Add(details);
                _context.SaveChanges();
                message = "News Details saved Successfully";
            }
            else
            {
                var details = _context.NewsDetails.FirstOrDefault(x => x.NewsId == model.NewsId);
                if (details != null)
                {
                    details.NewsDate = model.NewsDate;
                    details.NewsHeading = model.NewsHeading;
                    details.NewsSubHeading = model.NewsSubHeading;
                    details.NewsCategoryId = model.NewsCategoryId;
                    details.NewsContent = model.NewsContent;
                    details.NewsImage1 = model.NewsImage1;
                    details.NewsImage2 = model.NewsImage2;
                    details.NewsImage3 = model.NewsImage3;
                    details.NewsImage4 = model.NewsImage4;
                    details.EnteredBy = model.EnteredBy;
                    details.EnteredDate = DateTime.Now;
                    details.IsActive = model.IsActive;
                    _context.SaveChanges();
                    message = "News Details Update Successfully";
                }
            }
            return message;
        }

        public string DeleteNewsDetails(int newsId, ref ErrorResponseModel errorResponseModel)
        {
            string message = "";
            var newsEntity = _context.NewsDetails.FirstOrDefault(x => x.NewsId == newsId);
            if (newsEntity != null)
            {
                newsEntity.IsActive = false;
                _context.SaveChanges();
                message = " News Details Delete Successfully";
            }
            return message;
        }

        public List<NewDetailModel> GetNewsDetailsbyCategoryId(long newsCategoryId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var list = GetNewsDetailList(newsCategoryId);
            if (list.Count == 0)
            {
                errorResponseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                errorResponseModel.Message = "News Details not found";
            }
            return list;
        }

        private List<NewDetailModel> GetNewsDetailList(long? newsCategoryId)
        {
            var rows = (from news in _context.NewsDetails
                        join category in _context.NewsCategories
                        on news.NewsCategoryId equals category.NewsCategoryId
                        where news.IsActive == true && (newsCategoryId == null || news.NewsCategoryId == newsCategoryId)
                        select new
                        {
                            news.NewsId,
                            news.NewsCategoryId,
                            news.NewsDate,
                            news.NewsHeading,
                            news.NewsSubHeading,
                            news.NewsImage1,
                            news.NewsImage2,
                            news.NewsImage3,
                            news.NewsImage4,
                            news.EnteredBy,
                            news.EnteredDate,
                            category.NewsCategory1,
                            news.NewsContent,
                            news.IsActive
                        }).AsNoTracking().ToList();

            return rows.Select(item => new NewDetailModel
            {
                NewsId = item.NewsId,
                NewsDate = item.NewsDate.HasValue ? item.NewsDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                NewsHeading = item.NewsHeading,
                NewsSubHeading = item.NewsSubHeading,
                NewsCategoryId = item.NewsCategoryId,
                NewsContent = item.NewsContent,
                NewsImage1 = item.NewsImage1,
                NewsImage2 = item.NewsImage2,
                NewsImage3 = item.NewsImage3,
                NewsImage4 = item.NewsImage4,
                EnteredBy = item.EnteredBy,
                EnteredDate = item.EnteredDate,
                NewsCategory1 = item.NewsCategory1,
                IsActive = newsCategoryId == null ? null : item.IsActive,
            }).ToList();
        }
#nullable restore
        #endregion
    }
}
