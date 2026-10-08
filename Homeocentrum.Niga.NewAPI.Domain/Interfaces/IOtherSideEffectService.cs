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
    public interface IOtherSideEffectService
    {
        /// <summary>
        /// Interface is used to deactivate OtherSideEffect.
        /// </summary>
        /// <param name="otherSideEffectModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteOtherSideEffect(OtherSideEffectModel otherSideEffectModel, ref ErrorResponseModel errorResponseModel);
    }
}
