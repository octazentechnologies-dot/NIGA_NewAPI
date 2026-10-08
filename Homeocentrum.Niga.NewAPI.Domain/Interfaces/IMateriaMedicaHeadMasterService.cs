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
    public interface IMateriaMedicaHeadMasterService
    {
        /// <summary>remedyId
        /// Interface is used to save MateriaMedicaHead
        /// </summary>
        /// <param name="materiamedicaheadModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveMateriaMedicaHead(MateriaMedicaHeadMasterModel materiamedicaheadModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate MateriaMedicaHead.
        /// </summary>
        /// <param name="materiamedicaheadModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteMateriaMedicaHead(MateriaMedicaHeadMasterModel materiamedicaheadModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// To update differential materia medica status
        /// </summary>
        /// <param name="materiaMedicaHeadId"></param>
        /// <param name="differentialMMDefaultStatus"></param>
        /// <returns></returns>
        string UpdateDifferentialMateriaMedicadDefaultStatus(int materiaMedicaHeadId, bool differentialMMDefaultStatus);
    }
}
