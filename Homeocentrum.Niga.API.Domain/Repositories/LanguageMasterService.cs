#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class LanguageMasterService : ILanguageMasterService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public LanguageMasterService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        public List<LanguageMasterModel> GetLanguage(ref ErrorResponseModel errorResponseModel)
        {
           
                errorResponseModel = new ErrorResponseModel();
                var LanguageModelList = new List<LanguageMasterModel>();
                var languageEntityList = context.LanguageMasters.Where(x => x.IsDeleted == false).ToList();

                if (languageEntityList.Count == 0)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Language not found";
                }
            languageEntityList.ForEach(item =>
                {
                    LanguageModelList.Add(new LanguageMasterModel
                    {
                        LanguageId=item.LanguageId,
                        LanguageName =item.LanguageName,
                         Description=item.Description,
                        IsDeleted=item.IsDeleted

                    });
            });
                return LanguageModelList;
            
        }

        /// <summary>
        /// Method implementation for saving new Language
        /// </summary>
        /// <param name="languagemasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveLanguage(LanguageMasterModel languagemasterModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (languagemasterModel.LanguageId == 0)
            {
                LanguageMaster languageEntity = new LanguageMaster();
                languageEntity.LanguageId = languagemasterModel.LanguageId;
                languageEntity.LanguageName = languagemasterModel.LanguageName;
                languageEntity.Description = languagemasterModel.Description;
                languageEntity.IsDeleted = languagemasterModel.IsDeleted;
                context.LanguageMasters.Add(languageEntity);
                context.SaveChanges();
                Message = "Language Saved Successfully";
            }
            else
            {
                var languageEntity = context.LanguageMasters.FirstOrDefault(x => x.LanguageId == languagemasterModel.LanguageId);
                if (languageEntity != null)
                {


                    languageEntity.LanguageName = languagemasterModel.LanguageName;
                    languageEntity.Description = languagemasterModel.Description;
                    languageEntity.IsDeleted = languagemasterModel.IsDeleted;
                    context.SaveChanges();
                    Message = "Language Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete Language.
        /// </summary>
        /// <param name="languagemasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public string DeleteLanguage(LanguageMasterModel languagemasterModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var languageEntity = context.LanguageMasters.FirstOrDefault(x => x.LanguageId == languagemasterModel.LanguageId);
            if (languageEntity != null)
            {
                languageEntity.IsDeleted = true;
                // context.Remove(authorEntity);
                context.SaveChanges();
                Message = "Language Deleted Successfully";

            }
            return Message;
        }
    }
}
