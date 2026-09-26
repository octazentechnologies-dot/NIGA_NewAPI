using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using API.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
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
    }
}
