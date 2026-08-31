using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;
using Niga_Domain.Helpers;
using API.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Niga_Domain.Business.Implementation
{
    /// <summary>
    /// This is implementation  for the package operations 
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
    }
    }

