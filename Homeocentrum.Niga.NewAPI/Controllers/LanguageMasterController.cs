#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [OldApiContract]
    public class LanguageMasterController : BaseAPIController
    {
        ILanguageMasterService languageMasterService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="languageMasterService"></param>
        public LanguageMasterController(ILanguageMasterService _languageMasterService)
        {
            languageMasterService = _languageMasterService;
        }

        /// <summary>
        /// To Get all Language
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(LanguageMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetLanguage()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var LanguageModelList = languageMasterService.GetLanguage(ref errorResponseModel);

                if (LanguageModelList != null)
                {
                    return Ok(LanguageModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }

        }

        /// <summary>
        /// To add new Language 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(LanguageMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveLanguage(LanguageMasterModel languagemasterModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var languageModel = languageMasterService.SaveLanguage(languagemasterModel, ref errorResponseModel);

                if (languageModel != null)
                {
                    return Ok(languageModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete Language 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteLanguage")]
        [ProducesResponseType(typeof(LanguageMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteLanguage(LanguageMasterModel languagemasterModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var LanguageModel = languageMasterService.DeleteLanguage(languagemasterModel, ref errorResponseModel);

                if (LanguageModel != null)
                {
                    return Ok(LanguageModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}
