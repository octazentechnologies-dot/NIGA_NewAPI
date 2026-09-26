using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interface;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    public class SeriousSideEffectService : ISeriousSideEffectService
    {
        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public SeriousSideEffectService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        /// <summary>
        /// Method is used for delete SeriousSideEffect.
        /// </summary>
        /// <param name="seriousSideEffectModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteSeriousSideEffect(SeriousSideEffectModel seriousSideEffectModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var seriousSideEffectEntity = context.SeriousSideEffectMasters.FirstOrDefault(x => x.SeriousSideEffectId == seriousSideEffectModel.SeriousSideEffectId);
            if (seriousSideEffectEntity != null)
            {
                seriousSideEffectEntity.DeleteStatus = true;
                context.SaveChanges();
                Message = "SeriousSideEffect Deleted Successfully";
            }
            return Message;
        }
    }
}
