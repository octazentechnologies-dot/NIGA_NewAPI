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
    public class DiagnosisSystemService : IDiagnosisSystemService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DiagnosisSystemService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        public string DeleteDiagnosisSystem(DiagnosisSystemModel diagnosissystemModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var diagnosissystemEntity = context.DiagnosisSystems.FirstOrDefault(x => x.DiagnosisSystemId == diagnosissystemModel.DiagnosisSystemId);
            if (diagnosissystemEntity != null)
            {
                diagnosissystemEntity.IsActive = true;
                context.SaveChanges();
                Message = "DiagnosisSystem Deleted Successfully";
            }
            return Message;
        }

        public string SaveDiagnosisSystem(DiagnosisSystemModel diagnosissystemModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (diagnosissystemModel.DiagnosisSystemId == 0)
            {
                DiagnosisSystem diagnosissystemEntity = new DiagnosisSystem();
                diagnosissystemEntity.DiagnosisSystemName = diagnosissystemModel.DiagnosisSystemName;
                diagnosissystemEntity.Description = diagnosissystemModel.Description;
                diagnosissystemEntity.IsActive = false;
                context.DiagnosisSystems.Add(diagnosissystemEntity);
                context.SaveChanges();
                Message = " DiagnosisSystem Saved Successfully";
            }
            else
            {
                var diagnosissystemEntity = context.DiagnosisSystems.FirstOrDefault(x => x.DiagnosisSystemId == diagnosissystemModel.DiagnosisSystemId);
                if (diagnosissystemEntity != null)
                {

                    diagnosissystemEntity.DiagnosisSystemName = diagnosissystemModel.DiagnosisSystemName;
                    diagnosissystemEntity.Description = diagnosissystemModel.Description;
                    diagnosissystemEntity.IsActive = false;

                    context.SaveChanges();
                    Message = "DiagnosisSystem Updated Successfully";
                }
            }
            return Message;
        }
    }
}
