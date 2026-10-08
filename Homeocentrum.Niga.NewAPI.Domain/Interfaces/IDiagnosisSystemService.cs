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
    public interface IDiagnosisSystemService
    {
        /// <summary>
        /// Interface is used to save BodyPart
        /// </summary>
        /// <param name="diagnosissystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveDiagnosisSystem(DiagnosisSystemModel diagnosissystemModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate bodypart.
        /// </summary>
        /// <param name="diagnosissystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteDiagnosisSystem(DiagnosisSystemModel diagnosissystemModel, ref ErrorResponseModel errorResponseModel);
    }
}
