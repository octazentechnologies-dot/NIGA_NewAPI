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
    public class CountryService : ICountryService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public CountryService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method for getting all the countries
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<CountryModel> GetCountries( ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var countryModelList = new List<CountryModel>();
            var countryEntityList = context.CountryMasters.ToList();

            if (countryEntityList.Count==0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Country not found";
            }
            countryEntityList.ForEach(item =>
            {
                countryModelList.Add(new CountryModel
                {
                    CountryId = item.CountryId,
                    CountryName = item.CountryName,
                    EnteredBy = item.EnteredBy,
                    EnteredDate=item.EnteredDate,
                    ChangedBy=item.ChangedBy,
                    ChangedDate=item.ChangedDate,
                    DeleteStatus=item.DeleteStatus
                });
            });
            return countryModelList;
        }
    }
}
