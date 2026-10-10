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
    public interface IDiagnosisTherapeuticsDetailService
    {
        /// <summary>
        /// Interface is used to save BodyPart
        /// </summary>
        /// <param name="diagnosisTherapeuticsDetailModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveDiagnosisTherapeuticsDetail(DiagnosisTherapeuticsDetailModel diagnosisTherapeuticsDetailModel, ref ErrorResponseModel errorResponseModel);
    }
}
