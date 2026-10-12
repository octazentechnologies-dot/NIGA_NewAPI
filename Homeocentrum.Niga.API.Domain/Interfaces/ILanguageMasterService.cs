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

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    public interface ILanguageMasterService
    {
        /// <summary>
        /// interface for getting all the Language
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<LanguageMasterModel> GetLanguage(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save Language
        /// </summary>
        /// <param name="languagemasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveLanguage(LanguageMasterModel languagemasterModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Language
        /// </summary>
        /// <param name="languagemasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteLanguage(LanguageMasterModel languagemasterModel, ref ErrorResponseModel errorResponseModel);
    }
}
