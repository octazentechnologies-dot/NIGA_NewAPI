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
    public class IntensityService : IIntensityService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public IntensityService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method for getting all the Intensities
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<IntensityModel> GetIntensities( ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var intensityModelList = new List<IntensityModel>();
            var intensityEntityList = context.IntensityMasters.Where(x => x.DeleteStatus == false).ToList();

            if (intensityEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Intensity not found";
            }
            intensityEntityList.ForEach(item =>
            {
                intensityModelList.Add(new IntensityModel
                {
                    IntensityId = item.IntensityId,
                    IntensityNo = item.IntensityNo,                  
                    Description = item.Description,
                    EnteredDate = item.EnteredDate,
                    EnteredBy = item.EnteredBy,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    DeleteStatus = item.DeleteStatus
                });
            });
            return intensityModelList;
        }

        /// <summary>
        /// Method implementation for saving new Intensity
        /// </summary>
        /// <param name="intensityModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveIntensity(IntensityModel intensityModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (intensityModel.IntensityId == 0)
            {
                IntensityMaster intensityEntity = new IntensityMaster();

                intensityEntity.IntensityNo = intensityModel.IntensityNo;
                intensityEntity.Description = intensityModel.Description;
                intensityEntity.EnteredBy = intensityModel.EnteredBy;
                intensityEntity.EnteredDate = DateTime.Now;
                context.IntensityMasters.Add(intensityEntity);
                context.SaveChanges();
                Message = "Intensity Saved Successfully";
            }
            else
            {
                var intensityEntity = context.IntensityMasters.FirstOrDefault(x => x.IntensityId == intensityModel.IntensityId);
                if (intensityEntity != null)
                {


                    intensityEntity.IntensityNo = intensityModel.IntensityNo;
                    intensityEntity.Description = intensityModel.Description;
                    intensityEntity.ChangedBy = intensityModel.EnteredBy;
                    intensityEntity.ChangedDate = DateTime.Now;
                    context.SaveChanges();
                    Message = "Intensity Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete intensity.
        /// </summary>
        /// <param name="intensityModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteIntensity(IntensityModel intensityModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var intensityEntity = context.IntensityMasters.FirstOrDefault(x => x.IntensityId == intensityModel.IntensityId);
            if (intensityEntity != null)
            {
                intensityEntity.DeleteStatus = intensityModel.DeleteStatus;
                intensityEntity.ChangedBy = intensityModel.EnteredBy;
                intensityEntity.ChangedDate = DateTime.Now;
                context.SaveChanges();
                Message = "Intensity Deleted Successfully";
            }
            return Message;
        }
    }
}
