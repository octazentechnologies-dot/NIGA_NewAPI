using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using API.Helpers;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;

namespace Niga_Domain.Business.Implementation
{
    public class AllopathicDrugService : IAllopathicDrugService
    {
        private readonly NIGACentrumContext _context;

        public AllopathicDrugService(NIGACentrumContext context)
        {
            _context = context;
        }

        public AllopathicDrugModel GetAllopathicDrugById(long allopathicDrugId)
        {
            var listAllopathicDrugModel = new AllopathicDrugModel();
            var errorResponseModel = new ErrorResponseModel();
            var allopathicDrugEntity = (
                from allopathicDrug in _context.AllopathicDrugMasters
                join drugGroup in _context.DrugGroupMasters
                    on allopathicDrug.DrugGroupId equals drugGroup.DrugGroupId
                join drugSystem in _context.DrugSystemMasters
                    on drugGroup.DrugSystemId equals drugSystem.DrugSystemId
                where
                    allopathicDrug.AllopathicDrugId == allopathicDrugId
                    && allopathicDrug.DeleteStatus == false
                select new AllopathicDrugModel
                {
                    AllopathicDrugId = allopathicDrug.AllopathicDrugId,
                    AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                    DrugGroupId = drugGroup.DrugGroupId,
                    DrugGroupName = drugGroup.DrugGroupName,
                    DrugSystemId = drugSystem.DrugSystemId,
                    DrugSystemName = drugSystem.DrugSystemName,
                }
            ).FirstOrDefault();
            if (allopathicDrugEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug not found";
                return listAllopathicDrugModel;
            }

            return allopathicDrugEntity;
        }

        /// <summary>
        /// Method to get all the AllopathicDrug
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<PagedList<AllopathicDrugModel>> GetAllopathicDrugsAsync(
            ParameterParams parameterParams
        )
        {
            var query = (
                from drug in _context.AllopathicDrugMasters
                where drug.DeleteStatus == false
                select new AllopathicDrugModel
                {
                    DrugGroupId = drug.DrugGroupId,
                    DrugGroupName = drug.DrugGroup.DrugGroupName,
                    AllopathicDrugId = drug.AllopathicDrugId,
                    AllopathicDrugName = drug.AllopathicDrugName,
                    DeleteStatus = drug.DeleteStatus,
                    AdverseReactionModelList = (
                        from a in _context.AdverseReactionMasters
                        where a.AllopathicDrugId == drug.AllopathicDrugId && a.DeleteStatus == false
                        select new AdverseReactionModel
                        {
                            AdverseReactionId = (int)a.AdverseReactionId,
                            AllopathicDrugId = a.AllopathicDrugId,
                            AllopathicDrugName = drug.AllopathicDrugName,
                            AdverseReactionName = a.AdverseReactionName,
                            DeleteStatus = a.DeleteStatus,
                        }
                    ).ToList(),

                    OtherSideEffectModelList = (
                        from o in _context.OtherSideEffectMasters
                        where o.AllopathicDrugId == drug.AllopathicDrugId && o.DeleteStatus == false
                        select new OtherSideEffectModel
                        {
                            OtherSideEffectId = (int)o.OtherSideEffectId,
                            AllopathicDrugId = o.AllopathicDrugId,
                            AllopathicDrugName = drug.AllopathicDrugName,
                            OtherSideEffectName = o.OtherSideEffectName,
                            DeleteStatus = o.DeleteStatus,
                        }
                    ).ToList(),

                    SeriousSideEffectModelList = (
                        from s in _context.SeriousSideEffectMasters
                        where s.AllopathicDrugId == drug.AllopathicDrugId && s.DeleteStatus == false
                        select new SeriousSideEffectModel
                        {
                            SeriousSideEffectId = (int)s.SeriousSideEffectId,
                            AllopathicDrugId = s.AllopathicDrugId,
                            AllopathicDrugName = drug.AllopathicDrugName,
                            SeriousSideEffectName = s.SeriousSideEffectName,
                            DeleteStatus = s.DeleteStatus,
                        }
                    ).ToList(),
                }
            ).AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(parameterParams.search))
            {
                query = query.Where(x =>
                    x.AllopathicDrugName.ToLower().Contains(parameterParams.search.ToLower())
                );
            }

            // Return paginated result
            return await PagedList<AllopathicDrugModel>.CreateAsync(
                query.AsNoTracking(),
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        public async Task<PagedList<AdverseReactionModel>> GetAllAdverseReactionsAsync(
            ParameterParams parameterParams
        )
        {
            var query = _context
                .AdverseReactionMasters.AsNoTracking()
                .Where(x =>
                    x.DeleteStatus == false
                    && x.AllopathicDrugId == parameterParams.allopathicDrugId
                )
                .Select(a => new AdverseReactionModel
                {
                    AdverseReactionId = (int)a.AdverseReactionId,
                    AllopathicDrugId = a.AllopathicDrugId,
                    AllopathicDrugName = a.AllopathicDrug.AllopathicDrugName,
                    AdverseReactionName = a.AdverseReactionName,
                    DeleteStatus = a.DeleteStatus,
                });

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(parameterParams.search))
            {
                var search = parameterParams.search.Trim().ToLower();
                query = query.Where(x =>
                    x.AdverseReactionName.ToLower().Contains(search)
                    || x.AllopathicDrugName.ToLower().Contains(search)
                );
            }
            return await PagedList<AdverseReactionModel>.CreateAsync(
                query,
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        public async Task<PagedList<OtherSideEffectModel>> GetAllOtherSideEffectsAsync(
            ParameterParams parameterParams
        )
        {
            var query = _context
                .OtherSideEffectMasters.AsNoTracking()
                .Where(x =>
                    x.DeleteStatus == false
                    && x.AllopathicDrugId == parameterParams.allopathicDrugId
                )
                .Select(o => new OtherSideEffectModel
                {
                    OtherSideEffectId = (int)o.OtherSideEffectId,
                    AllopathicDrugId = o.AllopathicDrugId,
                    AllopathicDrugName = o.AllopathicDrug.AllopathicDrugName,
                    OtherSideEffectName = o.OtherSideEffectName,
                    DeleteStatus = o.DeleteStatus,
                });

            if (!string.IsNullOrWhiteSpace(parameterParams.search))
            {
                var search = parameterParams.search.Trim().ToLower();
                query = query.Where(x =>
                    x.OtherSideEffectName.ToLower().Contains(search)
                    || x.AllopathicDrugName.ToLower().Contains(search)
                );
            }

            return await PagedList<OtherSideEffectModel>.CreateAsync(
                query,
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        public async Task<PagedList<SeriousSideEffectModel>> GetAllSeriousSideEffectsAsync(
            ParameterParams parameterParams
        )
        {
            var query = _context
                .SeriousSideEffectMasters.AsNoTracking()
                .Where(x =>
                    x.DeleteStatus == false
                    && x.AllopathicDrugId == parameterParams.allopathicDrugId
                ).Select(s => new SeriousSideEffectModel
            {
                SeriousSideEffectId = (int)s.SeriousSideEffectId,
                AllopathicDrugId = s.AllopathicDrugId,
                AllopathicDrugName = s.AllopathicDrug.AllopathicDrugName,
                SeriousSideEffectName = s.SeriousSideEffectName,
                DeleteStatus = s.DeleteStatus,
            });

            if (!string.IsNullOrWhiteSpace(parameterParams.search))
            {
                var search = parameterParams.search.Trim().ToLower();
                query = query.Where(x =>
                    x.SeriousSideEffectName.ToLower().Contains(search)
                    || x.AllopathicDrugName.ToLower().Contains(search)
                );
            }

            return await PagedList<SeriousSideEffectModel>.CreateAsync(
                query,
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        /// <summary>
        /// Method implementation for saving new AllopathicDrug
        /// </summary>
        /// <param name="AllopathicDrugModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<string> SaveAllopathicDrug(AllopathicDrugModel allopathicDrugModel)
        {
            var errorResponseModel = new ErrorResponseModel();
            string Message = "";
            if (allopathicDrugModel.AllopathicDrugId == 0)
            {
                AllopathicDrugMaster allopathicDrugEntity = new AllopathicDrugMaster();
                allopathicDrugEntity.AllopathicDrugName = allopathicDrugModel.AllopathicDrugName;
                allopathicDrugEntity.DrugGroupId = allopathicDrugModel.DrugGroupId;
                allopathicDrugEntity.DeleteStatus = false;
                _context.AllopathicDrugMasters.Add(allopathicDrugEntity);
                _context.SaveChanges();
                allopathicDrugModel.AdverseReactionModelList.ForEach(item =>
                {
                    AdverseReactionMaster adverseReactionEntity = new AdverseReactionMaster();
                    adverseReactionEntity.AdverseReactionName = item.AdverseReactionName;
                    adverseReactionEntity.AllopathicDrugId = allopathicDrugEntity.AllopathicDrugId;
                    adverseReactionEntity.DeleteStatus = false;
                    _context.AdverseReactionMasters.Add(adverseReactionEntity);
                    _context.SaveChanges();
                });

                allopathicDrugModel.OtherSideEffectModelList.ForEach(item =>
                {
                    OtherSideEffectMaster otherSideEffectEntity = new OtherSideEffectMaster();
                    otherSideEffectEntity.OtherSideEffectName = item.OtherSideEffectName;
                    otherSideEffectEntity.AllopathicDrugId = allopathicDrugEntity.AllopathicDrugId;
                    otherSideEffectEntity.DeleteStatus = false;
                    _context.OtherSideEffectMasters.Add(otherSideEffectEntity);
                    _context.SaveChanges();
                });

                allopathicDrugModel.SeriousSideEffectModelList.ForEach(item =>
                {
                    SeriousSideEffectMaster seriousSideEffectEntity = new SeriousSideEffectMaster();
                    seriousSideEffectEntity.SeriousSideEffectName = item.SeriousSideEffectName;
                    seriousSideEffectEntity.AllopathicDrugId =
                        allopathicDrugEntity.AllopathicDrugId;
                    seriousSideEffectEntity.DeleteStatus = false;
                    _context.SeriousSideEffectMasters.Add(seriousSideEffectEntity);
                    _context.SaveChanges();
                });

                Message = "allopathicDrug Saved Successfully";
            }
            else
            {
                var allopathicDrugEntity = _context.AllopathicDrugMasters.FirstOrDefault(x =>
                    x.AllopathicDrugId == allopathicDrugModel.AllopathicDrugId
                );
                if (allopathicDrugEntity != null)
                {
                    allopathicDrugEntity.AllopathicDrugName =
                        allopathicDrugModel.AllopathicDrugName;
                    allopathicDrugEntity.DrugGroupId = allopathicDrugModel.DrugGroupId;
                    allopathicDrugEntity.DeleteStatus = false;
                    _context.SaveChanges();

                    allopathicDrugModel.AdverseReactionModelList.ForEach(item =>
                    {
                        var adverseReactionEntity = _context.AdverseReactionMasters.FirstOrDefault(
                            x =>
                                x.AdverseReactionId == item.AdverseReactionId
                                && x.DeleteStatus == false
                        );
                        if (adverseReactionEntity != null)
                        {
                            adverseReactionEntity.AdverseReactionId = item.AdverseReactionId;
                            adverseReactionEntity.AdverseReactionName = item.AdverseReactionName;
                            adverseReactionEntity.AllopathicDrugId =
                                allopathicDrugEntity.AllopathicDrugId;
                            adverseReactionEntity.DeleteStatus = false;
                            _context.SaveChanges();
                        }
                        else
                        {
                            AdverseReactionMaster _adverseReactionEntity =
                                new AdverseReactionMaster();
                            _adverseReactionEntity.AdverseReactionName = item.AdverseReactionName;
                            _adverseReactionEntity.AllopathicDrugId =
                                allopathicDrugEntity.AllopathicDrugId;
                            _adverseReactionEntity.DeleteStatus = false;
                            _context.AdverseReactionMasters.Add(_adverseReactionEntity);
                            _context.SaveChanges();
                        }
                    });

                    allopathicDrugModel.OtherSideEffectModelList.ForEach(item =>
                    {
                        var otherSideEffectEntity = _context.OtherSideEffectMasters.FirstOrDefault(
                            x =>
                                x.OtherSideEffectId == item.OtherSideEffectId
                                && x.DeleteStatus == false
                        );

                        if (otherSideEffectEntity != null)
                        {
                            otherSideEffectEntity.OtherSideEffectId = item.OtherSideEffectId;
                            otherSideEffectEntity.OtherSideEffectName = item.OtherSideEffectName;
                            otherSideEffectEntity.AllopathicDrugId =
                                allopathicDrugEntity.AllopathicDrugId;
                            otherSideEffectEntity.DeleteStatus = false;
                            _context.SaveChanges();
                        }
                        else
                        {
                            OtherSideEffectMaster _otherSideEffectEntity =
                                new OtherSideEffectMaster();
                            _otherSideEffectEntity.OtherSideEffectName = item.OtherSideEffectName;
                            _otherSideEffectEntity.AllopathicDrugId =
                                allopathicDrugEntity.AllopathicDrugId;
                            _otherSideEffectEntity.DeleteStatus = false;
                            _context.OtherSideEffectMasters.Add(_otherSideEffectEntity);
                            _context.SaveChanges();
                        }
                    });

                    allopathicDrugModel.SeriousSideEffectModelList.ForEach(item =>
                    {
                        var seriousSideEffectEntity =
                            _context.SeriousSideEffectMasters.FirstOrDefault(x =>
                                x.SeriousSideEffectId == item.SeriousSideEffectId
                                && x.DeleteStatus == false
                            );

                        if (seriousSideEffectEntity != null)
                        {
                            seriousSideEffectEntity.SeriousSideEffectId = item.SeriousSideEffectId;
                            seriousSideEffectEntity.SeriousSideEffectName =
                                item.SeriousSideEffectName;
                            seriousSideEffectEntity.AllopathicDrugId =
                                allopathicDrugEntity.AllopathicDrugId;
                            seriousSideEffectEntity.DeleteStatus = false;
                            _context.SaveChanges();
                        }
                        else
                        {
                            SeriousSideEffectMaster _seriousSideEffectEntity =
                                new SeriousSideEffectMaster();
                            _seriousSideEffectEntity.SeriousSideEffectName =
                                item.SeriousSideEffectName;
                            _seriousSideEffectEntity.AllopathicDrugId =
                                allopathicDrugEntity.AllopathicDrugId;
                            _seriousSideEffectEntity.DeleteStatus = false;
                            _context.SeriousSideEffectMasters.Add(_seriousSideEffectEntity);
                            _context.SaveChanges();
                        }
                    });

                    Message = "allopathicDrug Updated Successfully";
                }
            }
            return Message;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Method is used for delete AllopathicDrug.
        /// </summary>
        /// <param name="qualificationModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<string> DeleteAllopathicDrug(long allopathicDrugId)
        {
            var errorResponseModel = new ErrorResponseModel();
            string Message = "";
            var allopathicDrugEntity = _context.AllopathicDrugMasters.FirstOrDefault(x =>
                x.AllopathicDrugId == allopathicDrugId
            );
            if (allopathicDrugEntity != null)
            {
                allopathicDrugEntity.DeleteStatus = true;
                await SaveAllAsync();
                Message = "AllopathicDrug Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Methood to get AllopathicDrug by adverseReactionId
        /// </summary>
        /// <param name="allopathicDrugId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<AllopathicDrugModel> GetAllopathicDrugByNameAsync(
            string allopathicDrugName
        )
        {
            var drug = await _context
                .AllopathicDrugMasters.Include(x => x.DrugGroup)
                .FirstOrDefaultAsync(x =>
                    x.AllopathicDrugName == allopathicDrugName && x.DeleteStatus == false
                );

            if (drug == null)
                return null;

            var model = new AllopathicDrugModel
            {
                AllopathicDrugId = drug.AllopathicDrugId,
                AllopathicDrugName = drug.AllopathicDrugName,
                DrugGroupId = drug.DrugGroupId,
                DrugGroupName = drug.DrugGroup?.DrugGroupName,
                DeleteStatus = drug.DeleteStatus,
                AdverseReactionModelList = await _context
                    .AdverseReactionMasters.Where(a =>
                        a.AllopathicDrugId == drug.AllopathicDrugId && a.DeleteStatus == false
                    )
                    .Select(a => new AdverseReactionModel
                    {
                        AdverseReactionId = (int)a.AdverseReactionId,
                        AllopathicDrugId = a.AllopathicDrugId,
                        AllopathicDrugName = drug.AllopathicDrugName,
                        AdverseReactionName = a.AdverseReactionName,
                        DeleteStatus = a.DeleteStatus,
                    })
                    .ToListAsync(),
                OtherSideEffectModelList = await _context
                    .OtherSideEffectMasters.Where(o =>
                        o.AllopathicDrugId == drug.AllopathicDrugId && o.DeleteStatus == false
                    )
                    .Select(o => new OtherSideEffectModel
                    {
                        OtherSideEffectId = (int)o.OtherSideEffectId,
                        AllopathicDrugId = o.AllopathicDrugId,
                        AllopathicDrugName = drug.AllopathicDrugName,
                        OtherSideEffectName = o.OtherSideEffectName,
                        DeleteStatus = o.DeleteStatus,
                    })
                    .ToListAsync(),
                SeriousSideEffectModelList = await _context
                    .SeriousSideEffectMasters.Where(s =>
                        s.AllopathicDrugId == drug.AllopathicDrugId && s.DeleteStatus == false
                    )
                    .Select(s => new SeriousSideEffectModel
                    {
                        SeriousSideEffectId = (int)s.SeriousSideEffectId,
                        AllopathicDrugId = s.AllopathicDrugId,
                        AllopathicDrugName = drug.AllopathicDrugName,
                        SeriousSideEffectName = s.SeriousSideEffectName,
                        DeleteStatus = s.DeleteStatus,
                    })
                    .ToListAsync(),
            };
            return model;
        }

        public async Task<List<AllopathicDrugDDModel>> GetAllopathicDrugDropdownAsync(
            string search = null
        )
        {
            var query = _context.AllopathicDrugMasters.Where(x => x.DeleteStatus == false);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.AllopathicDrugName.Contains(search));
            var list = await query
                .Select(x => new AllopathicDrugDDModel
                {
                    AllopathicDrugId = x.AllopathicDrugId,
                    AllopathicDrugName = x.AllopathicDrugName,
                })
                .ToListAsync();
            return list;
        }

        /// <summary>
        /// Methood to get AllopathicDrug by adverseReactionId
        /// </summary>
        /// <param name="allopathicDrugId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public AllopathicDrugModel GetAllopathicDrugByID(
            int allopathicDrugId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var listAllopathicDrugModel = new AllopathicDrugModel();
            errorResponseModel = new ErrorResponseModel();

            var allopathicDrugEntity = (
                from allopathicDrug in _context.AllopathicDrugMasters
                join drugGroup in _context.DrugGroupMasters
                    on allopathicDrug.DrugGroupId equals drugGroup.DrugGroupId
                join drugSystem in _context.DrugSystemMasters
                    on drugGroup.DrugSystemId equals drugSystem.DrugSystemId
                where
                    allopathicDrug.AllopathicDrugId == allopathicDrugId
                    && allopathicDrug.DeleteStatus == false
                select new AllopathicDrugModel
                {
                    AllopathicDrugId = allopathicDrug.AllopathicDrugId,
                    AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                    DrugGroupId = drugGroup.DrugGroupId,
                    DrugGroupName = drugGroup.DrugGroupName,
                    DrugSystemId = drugSystem.DrugSystemId,
                    DrugSystemName = drugSystem.DrugSystemName,
                }
            ).FirstOrDefault();
            if (allopathicDrugEntity != null)
            {
                //Get all recodrs from AdverseReactionMasters join with AllopathicDrugMasters on allopathicDrugId
                var adverseReactionEntity = (
                    from AdverseReactionMasters in _context.AdverseReactionMasters
                    join allopathicDrug in _context.AllopathicDrugMasters
                        on AdverseReactionMasters.AllopathicDrugId equals allopathicDrug.AllopathicDrugId
                    where
                        allopathicDrug.AllopathicDrugId == allopathicDrugEntity.AllopathicDrugId
                        && AdverseReactionMasters.DeleteStatus == false
                    select new AdverseReactionModel
                    {
                        AdverseReactionId = (int)AdverseReactionMasters.AdverseReactionId,
                        AllopathicDrugId = AdverseReactionMasters.AllopathicDrugId,
                        AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                        AdverseReactionName = AdverseReactionMasters.AdverseReactionName,
                        DeleteStatus = AdverseReactionMasters.DeleteStatus,
                    }
                ).ToList();

                var otherSideEffectEntity = (
                    from otherSideEffect in _context.OtherSideEffectMasters
                    join allopathicDrug in _context.AllopathicDrugMasters
                        on otherSideEffect.AllopathicDrugId equals allopathicDrug.AllopathicDrugId
                    where
                        allopathicDrug.AllopathicDrugId == allopathicDrugEntity.AllopathicDrugId
                        && otherSideEffect.DeleteStatus == false
                    select new OtherSideEffectModel
                    {
                        OtherSideEffectId = (int)otherSideEffect.OtherSideEffectId,
                        AllopathicDrugId = otherSideEffect.AllopathicDrugId,
                        AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                        OtherSideEffectName = otherSideEffect.OtherSideEffectName,
                        DeleteStatus = otherSideEffect.DeleteStatus,
                    }
                ).ToList();

                var seriousSideEffectEntity = (
                    from seriousSideEffect in _context.SeriousSideEffectMasters
                    join allopathicDrug in _context.AllopathicDrugMasters
                        on seriousSideEffect.AllopathicDrugId equals allopathicDrug.AllopathicDrugId
                    where
                        allopathicDrug.AllopathicDrugId == allopathicDrugEntity.AllopathicDrugId
                        && seriousSideEffect.DeleteStatus == false
                    select new SeriousSideEffectModel
                    {
                        SeriousSideEffectId = (int)seriousSideEffect.SeriousSideEffectId,
                        AllopathicDrugId = seriousSideEffect.AllopathicDrugId,
                        AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                        SeriousSideEffectName = seriousSideEffect.SeriousSideEffectName,
                        DeleteStatus = seriousSideEffect.DeleteStatus,
                    }
                ).ToList();
                allopathicDrugEntity.AdverseReactionModelList = adverseReactionEntity;
                allopathicDrugEntity.OtherSideEffectModelList = otherSideEffectEntity;
                allopathicDrugEntity.SeriousSideEffectModelList = seriousSideEffectEntity;
            }
            else
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section not found";
            }
            return allopathicDrugEntity;
        }
    }
}
