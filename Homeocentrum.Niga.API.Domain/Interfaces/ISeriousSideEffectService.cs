using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interface
{
    /// <summary>
    /// Interface used for SeriousSideEffect related operations
    /// </summary>
    public interface ISeriousSideEffectService
    {
        /// <summary>
        /// Interface is used to deactivate SeriousSideEffect.
        /// </summary>
        /// <param name="seriousSideEffectModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        
        string DeleteSeriousSideEffect(SeriousSideEffectModel seriousSideEffectModel, ref ErrorResponseModel errorResponseModel);


    }
}
