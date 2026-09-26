using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
    /// <summary>
    /// Interface used for state related operations
    /// </summary>
   public interface IStateService
    {
        /// <summary>
        /// Method is used for to get state by StateId
        /// </summary>
        /// <param name="stateId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        StateModel GetStateById(long stateId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface for getting states by countryId
        /// </summary>
        /// <param name="countryId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<StateModel> GetStates(ref ErrorResponseModel errorResponseModel);

        ///// <summary>
        ///// Interface is used to save State
        ///// </summary>
        ///// <param name="stateModel"></param>
        ///// <param name="errorResponseModel"></param>
        ///// <returns></returns>
        //string SaveState(StateModel stateModel, ref ErrorResponseModel errorResponseModel);

        ///// <summary>
        ///// Interface is used to deactivate State.
        ///// </summary>
        ///// <param name="stateModel"></param>
        ///// <param name="errorResponseModel"></param>
        ///// <returns></returns>
        //string DeleteState(StateModel stateModel, ref ErrorResponseModel errorResponseModel);
    }
}
