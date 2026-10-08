#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    public interface IIntensityService
    {
        /// <summary>
        /// Method is used for get all the Intensities
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<IntensityModel> GetIntensities(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save Intensity
        /// </summary>
        /// <param name="intensityModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveIntensity(IntensityModel intensityModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Intensity.
        /// </summary>
        /// <param name="intensityModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteIntensity(IntensityModel intensityModel, ref ErrorResponseModel errorResponseModel);
    }
}
