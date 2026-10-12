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
    public interface IDrugSystemService
    {
        /// <summary>
        /// Interface is used to save DrugSystem
        /// </summary>
        /// <param name="drugSystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveDrugSystem(DrugSystemModel drugSystemModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate DrugSystem.
        /// </summary>
        /// <param name="drugSystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteDrugSystem(DrugSystemModel drugSystemModel, ref ErrorResponseModel errorResponseModel);
    }
}
