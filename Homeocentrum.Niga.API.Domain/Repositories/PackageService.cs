using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Helpers;
using API.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    /// <summary>
    /// Package / subscription plan operations.
    /// M02 W7 ADM-B03: PackageMaster + PackageEntryDetail are S1 SaaS subscription only —
    /// never reuse PackageEntryDetail for S2 consult or S5 medicine billing.
    /// </summary>
    public class PackageService : IPackageService
    {
        private readonly NIGACentrumContext _context;

        public PackageService(NIGACentrumContext centrumContext)
        {
            _context = centrumContext;
        }

        public async Task<PackageModel?> GetPackageByIdAsync(long packageId)
        {
            var packageEntity = await _context.PackageMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PackageId == packageId && !x.DeleteStatus);
            if (packageEntity == null) return null;
            return new PackageModel
            {
                PackageId = packageEntity.PackageId,
                PackageName = packageEntity.PackageName,
                CaseCount = packageEntity.CaseCount,
                ValidityInDays = packageEntity.ValidityInDays,
                Amount = packageEntity.Amount,
                EnteredDate = packageEntity.EnteredDate,
                EnteredBy = packageEntity.EnteredBy,
                ChangedBy = packageEntity.ChangedBy,
                ChangedDate = packageEntity.ChangedDate,
                DeleteStatus = packageEntity.DeleteStatus,
            };
        }

        public async Task<PagedList<PackageModel>> GetAllPackagesAsync(ParameterParams parameterParams)
        {
            var query = _context.PackageMasters
                .AsNoTracking()
                .Where(x => !x.DeleteStatus);

            // Optional: Add search/sort logic here if needed

            var projected = query.Select(item => new PackageModel
            {
                PackageId = item.PackageId,
                PackageName = item.PackageName,
                CaseCount = item.CaseCount,
                ValidityInDays = item.ValidityInDays,
                Amount = item.Amount,
                EnteredDate = item.EnteredDate,
                EnteredBy = item.EnteredBy,
                ChangedBy = item.ChangedBy,
                ChangedDate = item.ChangedDate,
                DeleteStatus = item.DeleteStatus
            });

            return await PagedList<PackageModel>.CreateAsync(
                projected,
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        public async Task<string> SavePackageAsync(PackageModel packageModel)
        {
            if (packageModel.PackageId == 0)
            {
                var packageEntity = new PackageMaster
                {
                    PackageName = packageModel.PackageName,
                    CaseCount = packageModel.CaseCount,
                    ValidityInDays = packageModel.ValidityInDays,
                    Amount = packageModel.Amount,
                    EnteredBy = packageModel.EnteredBy,
                    EnteredDate = DateTime.Now,
                    DeleteStatus = false
                };
                await _context.PackageMasters.AddAsync(packageEntity);
                await _context.SaveChangesAsync();
                return "Package Saved Successfully";
            }
            else
            {
                var packageEntity = await _context.PackageMasters.FirstOrDefaultAsync(x => x.PackageId == packageModel.PackageId);
                if (packageEntity != null)
                {
                    packageEntity.PackageName = packageModel.PackageName;
                    packageEntity.CaseCount = packageModel.CaseCount;
                    packageEntity.ValidityInDays = packageModel.ValidityInDays;
                    packageEntity.Amount = packageModel.Amount;
                    packageEntity.ChangedBy = packageModel.EnteredBy;
                    packageEntity.ChangedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return "Package Updated Successfully";
                }
                return "Package not found";
            }
        }

        public async Task<string> DeletePackageAsync(long packageId, string changedBy)
        {
            var packageEntity = await _context.PackageMasters.FirstOrDefaultAsync(x => x.PackageId == packageId);
            if (packageEntity != null)
            {
                packageEntity.DeleteStatus = true;
                packageEntity.ChangedBy = changedBy;
                packageEntity.ChangedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return "Package Deleted Successfully";
            }
            return "Package not found";
        }

        public async Task<PagedList<PackageTopupModel>> GetAllPackageTopupsAsync(ParameterParams parameterParams)
        {
            var query = _context.PackageTopupMasters
                .AsNoTracking()
                .Where(x => !x.DeleteStatus)
                .Select(packageTopup => new PackageTopupModel
                {
                    PackageTopupId = packageTopup.PackageTopupId,
                    PackageTopupName = packageTopup.PackageTopupName,
                    CaseCount = packageTopup.CaseCount,
                    TopupAmount = packageTopup.Amount,
                    EnteredBy = packageTopup.EnteredBy,
                    ChangedBy = packageTopup.ChangedBy
                });

            return await PagedList<PackageTopupModel>.CreateAsync(
                query,
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        

        public async Task<string> SavePackageTopupAsync(PackageTopupModel packageTopupModel)
        {
            var packageTopupEntity = new PackageTopupMaster
            {
                PackageTopupName = packageTopupModel.PackageTopupName,
                CaseCount = packageTopupModel.CaseCount,
                Amount = packageTopupModel.TopupAmount,
                EnteredBy = packageTopupModel.EnteredBy,
                EnteredDate = DateTime.Now,
                DeleteStatus = false
            };
            await _context.PackageTopupMasters.AddAsync(packageTopupEntity);
            await _context.SaveChangesAsync();
            return "Package Topup Saved Successfully";
        }


        #region Old API compatible methods
#nullable disable

        /// <summary>
        /// Method is used for delete Package.
        /// </summary>
        /// <param name="packageModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeletePackage(PackageModel packageModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var packageEntity = _context.PackageMasters.FirstOrDefault(x => x.PackageId == packageModel.PackageId);
            if (packageEntity != null)
            {
                packageEntity.DeleteStatus = packageModel.DeleteStatus;
                packageEntity.ChangedBy = packageModel.EnteredBy;
                packageEntity.ChangedDate = DateTime.Now;
                _context.SaveChanges();
                Message = "Package Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Method for getting all the Packages
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<PackageModel> GetPackages(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var packageModelList = new List<PackageModel>();
            var packageEntityList = _context.PackageMasters.Where(x => x.DeleteStatus == false).ToList();
            if (packageEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Package not found";
            }
            packageEntityList.ForEach(item =>
            {
                packageModelList.Add(new PackageModel
                {
                    PackageId = item.PackageId,
                    PackageName = item.PackageName,
                    CaseCount = item.CaseCount,
                    ValidityInDays = item.ValidityInDays,
                    Amount = item.Amount,
                    EnteredDate = item.EnteredDate,
                    EnteredBy = item.EnteredBy,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    DeleteStatus = item.DeleteStatus
                });
            });
            return packageModelList;
        }

        /// <summary>
        /// Method implementation for saving new Package
        /// </summary>
        /// <param name="packageModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SavePackage(PackageModel packageModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (packageModel.PackageId == 0)
            {
                PackageMaster packageEntity = new PackageMaster();
                packageEntity.PackageName = packageModel.PackageName;
                packageEntity.CaseCount = packageModel.CaseCount;
                packageEntity.ValidityInDays = packageModel.ValidityInDays;
                packageEntity.Amount = packageModel.Amount;
                packageEntity.EnteredBy = packageModel.EnteredBy;
                packageEntity.EnteredDate = DateTime.Now;
                _context.PackageMasters.Add(packageEntity);
                _context.SaveChanges();
                Message = "Package Saved Successfully";
            }
            else
            {
                var packageEntity = _context.PackageMasters.FirstOrDefault(x => x.PackageId == packageModel.PackageId);
                if (packageEntity != null)
                {

                    packageEntity.PackageName = packageModel.PackageName;
                    packageEntity.CaseCount = packageModel.CaseCount;
                    packageEntity.ValidityInDays = packageModel.ValidityInDays;
                    packageEntity.Amount = packageModel.Amount;
                    packageEntity.ChangedBy = packageModel.EnteredBy;
                    packageEntity.ChangedDate = DateTime.Now;
                    _context.SaveChanges();
                    Message = "Package Updated Successfully";
                }
            }
            return Message;
        }

#nullable restore
        #endregion
    }
    }

