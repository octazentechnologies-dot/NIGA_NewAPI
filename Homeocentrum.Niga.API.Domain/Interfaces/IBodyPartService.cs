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
    public interface IBodyPartService
    {
        /// <summary>
        /// Interface is used to save BodyPart
        /// </summary>
        /// <param name="bodypartModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveBodyPart(BodyPartModel bodypartModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate bodypart.
        /// </summary>
        /// <param name="bodypartModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteBodyPart(BodyPartModel bodypartModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the bodyparts by section
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<BodyPartModel> GetBodyPartBySection(long sectionId,ref ErrorResponseModel errorResponseModel);
    }
}
