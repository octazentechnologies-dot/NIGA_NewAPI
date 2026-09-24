using Homeocentrum.Niga.NewAPI.Domain.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Data;

namespace Homeocentrum.Niga.NewAPI.Domain.Implementation
{
    /// <summary>
    /// This is implementation  for the state operations 
    /// </summary>
    public class StateService : IStateService
    {
        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public StateService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        /// <summary>
        /// Method to get state by state id
        /// </summary>
        /// <param name="stateId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public StateModel GetStateById(long stateId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var stateEntity = context.StateMasters.FirstOrDefault(x => x.StateId == stateId && !x.DeleteStatus);
            if (stateEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "State not found";
            }
            return new StateModel
            {
                StateId = stateEntity.StateId,
                StateName = stateEntity.StateName,
                EnteredDate = stateEntity.EnteredDate,
                EnteredBy = stateEntity.EnteredBy,
                ChangedBy = stateEntity.ChangedBy,
                ChangedDate = stateEntity.ChangedDate,
                DeleteStatus = stateEntity.DeleteStatus,
            };
        }

        /// <summary>
        /// Method to get states by countryId
        /// </summary>
        /// <param name="countryId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<StateModel> GetStates(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var stateModelList = new List<StateModel>();
             stateModelList= (from item in context.StateMasters
                    where item.DeleteStatus==false
                    select new StateModel
                    {
                    StateId = item.StateId,
                    StateName = item.StateName,
                    EnteredDate = item.EnteredDate,
                    EnteredBy = item.EnteredBy,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    DeleteStatus = item.DeleteStatus,
                    CountryId = item.CountryId,
                }).ToList();
            if (stateModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "State not found";
            }
            return stateModelList;
        }
    }
}
