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
    public class DiagnosisTherapeuticsDetailService : IDiagnosisTherapeuticsDetailService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DiagnosisTherapeuticsDetailService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        public string SaveDiagnosisTherapeuticsDetail(DiagnosisTherapeuticsDetailModel diagnosisTherapeuticsDetailModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (diagnosisTherapeuticsDetailModel.DiagnosisTherapeuticsDetailId == 0)
            {
                DiagnosisTherapeuticsDetail diagnosisTherapeuticsDetailEntity = new DiagnosisTherapeuticsDetail();
                diagnosisTherapeuticsDetailEntity.DiagnosisId = diagnosisTherapeuticsDetailModel.DiagnosisId;
                diagnosisTherapeuticsDetailEntity.DiagnosisTherapeuticsDetail1 = diagnosisTherapeuticsDetailModel.DiagnosisTherapeuticsDetail1;
                diagnosisTherapeuticsDetailEntity.DeletedStatus = false;
                context.DiagnosisTherapeuticsDetails.Add(diagnosisTherapeuticsDetailEntity);
                context.SaveChanges();
                Message = " DiagnosisTherapeuticsDetail Saved Successfully";
            }
            else
            {
                var diagnosisTherapeuticsDetailEntity = context.DiagnosisTherapeuticsDetails.FirstOrDefault(x => x.DiagnosisTherapeuticsDetailId == diagnosisTherapeuticsDetailModel.DiagnosisTherapeuticsDetailId);
                if (diagnosisTherapeuticsDetailEntity != null)
                {

                    diagnosisTherapeuticsDetailEntity.DiagnosisId = diagnosisTherapeuticsDetailModel.DiagnosisId;
                    diagnosisTherapeuticsDetailEntity.DiagnosisTherapeuticsDetail1 = diagnosisTherapeuticsDetailModel.DiagnosisTherapeuticsDetail1;
                    diagnosisTherapeuticsDetailEntity.DeletedStatus = false;
                    context.SaveChanges();
                    Message = "DiagnosisTherapeuticsDetail Updated Successfully";
                }
            }
            return Message;
        }
    }
}
