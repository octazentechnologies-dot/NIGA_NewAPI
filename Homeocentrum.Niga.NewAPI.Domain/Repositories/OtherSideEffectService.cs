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

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
{
    public class OtherSideEffectService : IOtherSideEffectService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public OtherSideEffectService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method is used for delete OtherSideEffect.
        /// </summary>
        /// <param name="otherSideEffectModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteOtherSideEffect(OtherSideEffectModel otherSideEffectModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var otherSideEffectEntity = context.OtherSideEffectMasters.FirstOrDefault(x => x.OtherSideEffectId == otherSideEffectModel.OtherSideEffectId);
            if (otherSideEffectEntity != null)
            {
                otherSideEffectEntity.DeleteStatus = true;
                context.SaveChanges();
                Message = "OtherSideEffect Deleted Successfully";
            }
            return Message;
        }
    }
}
