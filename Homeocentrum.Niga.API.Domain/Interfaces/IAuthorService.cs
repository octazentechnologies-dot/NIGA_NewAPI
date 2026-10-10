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
    public interface IAuthorService
    {
        /// <summary>
        /// interface for getting all the Author
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<AuthorMasterModel> GetAuthor(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save Author
        /// </summary>
        /// <param name="authormasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveAuthor(AuthorMasterModel authormasterModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Author.
        /// </summary>
        /// <param name="authormasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteAuthor(AuthorMasterModel authormasterModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the Author for Repertory
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<AuthorMasterModel> GetAuthorforRepertory(ref ErrorResponseModel errorResponseModel);
    }
}
