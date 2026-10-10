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
    public interface IAdverseReactionService
    {
        ///// <summary>
        ///// Method is used for to get adverseReaction by adverseReactionID
        ///// </summary>
        ///// <param name="adverseReactionId"></param>
        ///// <param name="errorResponseModel"></param>
        ///// <returns></returns>
        //AdverseReactionModel GetAdverseReactionnById(long adverseReactionId, ref ErrorResponseModel errorResponseModel);

        ///// <summary>
        ///// Method is used for get all the adverseReaction
        ///// </summary>
        ///// <param name=""></param>
        ///// <returns></returns>
        //List<AdverseReactionModel> GetAdverseReaction(ref ErrorResponseModel errorResponseModel);

        ///// <summary>
        ///// Interface is used to save adverseReaction
        ///// </summary>
        ///// <param name="adverseReactionModel"></param>
        ///// <param name="errorResponseModel"></param>
        ///// <returns></returns>
        //string SaveAdverseReaction(AdverseReactionModel adverseReactionModel, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to deactivate adverseReaction.
        /// </summary>
        /// <param name="adverseReactionModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteAdverseReaction(AdverseReactionModel adverseReactionModel, ref ErrorResponseModel errorResponseModel);
    }
}
