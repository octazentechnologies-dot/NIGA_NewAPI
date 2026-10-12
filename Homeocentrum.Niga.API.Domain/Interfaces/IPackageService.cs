using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using API.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    /// <summary>
    /// Interface used for package related operations
    /// </summary>
    public interface IPackageService
    {
        Task<PackageModel> GetPackageByIdAsync(long packageId);
        Task<PagedList<PackageModel>> GetAllPackagesAsync(ParameterParams parameterParams);
        Task<string> SavePackageAsync(PackageModel packageModel);
        Task<string> DeletePackageAsync(long packageId, string changedBy);
        Task<PagedList<PackageTopupModel>> GetAllPackageTopupsAsync(ParameterParams parameterParams);
        Task<string> SavePackageTopupAsync(PackageTopupModel packageTopupModel);

        #region Old API compatible methods
#nullable disable
        /// <summary>
        /// Method is used for get all the Packages
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<PackageModel> GetPackages(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save Package
        /// </summary>
        /// <param name="packageModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SavePackage(PackageModel packageModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Package.
        /// </summary>
        /// <param name="packageModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeletePackage(PackageModel packageModel, ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }
}
