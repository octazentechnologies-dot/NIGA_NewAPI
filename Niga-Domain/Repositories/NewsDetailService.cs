using Microsoft.EntityFrameworkCore;
using Niga_Domain.Business.Interface;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;
using AutoMapper;
using Niga_Domain.Data;
using API.Helpers;

namespace Niga_Domain.Business.Implementation
{
    public class NewsDetailService : INewsDetailService
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        public NewsDetailService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

        public async Task<NewDetailModel1> GetNewsDetailsById(long newsId)
        {
            var news = await _context.NewsDetails.FirstOrDefaultAsync(x => x.NewsId == newsId && x.IsActive == true);
            return _mapper.Map<NewDetailModel1>(news);
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
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data", folderName);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid().ToString() + ".png";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
            return Path.Combine(folderName, fileName);
        }

        public NewDetailModel1 GetNewsDetailsbyId(long newsId, ref ErrorResponseModel errorResponseModel)
        {
            throw new NotImplementedException();
        }

        public List<NewDetailModel> GetAllNewsDetails(ref ErrorResponseModel errorResponseModel)
        {
            throw new NotImplementedException();
        }

        public string SaveNewsDetails(NewDetailModel1 model, ref ErrorResponseModel errorResponseModel)
        {
            throw new NotImplementedException();
        }

        public string DeleteNewsDetails(int newsId, ref ErrorResponseModel errorResponseModel)
        {
            throw new NotImplementedException();
        }

        public List<NewDetailModel> GetNewsDetailsbyCategoryId(long newsCategoryId, ref ErrorResponseModel errorResponseModel)
        {
            throw new NotImplementedException();
        }
    }
}
