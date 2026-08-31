using System;
using System.Collections.Generic;
using System.Text;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interface
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
