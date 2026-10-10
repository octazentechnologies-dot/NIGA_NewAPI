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
    public class AdverseReactionService : IAdverseReactionService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public AdverseReactionService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method is used for delete AdverseReaction.
        /// </summary>
        /// <param name="qualificationModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteAdverseReaction(AdverseReactionModel adverseReactionModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var adverseReactionEntity = context.AdverseReactionMasters.FirstOrDefault(x => x.AdverseReactionId == adverseReactionModel.AdverseReactionId);
            if (adverseReactionEntity != null)
            {
                adverseReactionEntity.DeleteStatus = true;
                context.SaveChanges();
                Message = "AdverseReaction Deleted Successfully";
            }
            return Message;
        }
    }
}
