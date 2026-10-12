using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using API.Helpers;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
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
                    DeleteStatus = allopathicDrug.DeleteStatus,
                    TotalAdverseReactions = _context.AdverseReactionMasters.Count(a =>
                        a.AllopathicDrugId == allopathicDrug.AllopathicDrugId && a.DeleteStatus == false
                    ),
                    TotalOtherSideEffects = _context.OtherSideEffectMasters.Count(o =>
                        o.AllopathicDrugId == allopathicDrug.AllopathicDrugId && o.DeleteStatus == false
                    ),
                    TotalSeriousSideEffects = _context.SeriousSideEffectMasters.Count(s =>
                        s.AllopathicDrugId == allopathicDrug.AllopathicDrugId && s.DeleteStatus == false
                    ),
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
                    DrugSystemId = drug.DrugGroup.DrugSystemId,
                    DrugSystemName = drug.DrugGroup.DrugSystem.DrugSystemName,
                    AllopathicDrugId = drug.AllopathicDrugId,
                    AllopathicDrugName = drug.AllopathicDrugName,
                    DeleteStatus = drug.DeleteStatus,
                    TotalAdverseReactions = _context.AdverseReactionMasters.Count(a =>
                        a.AllopathicDrugId == drug.AllopathicDrugId && a.DeleteStatus == false
                    ),
                    TotalOtherSideEffects = _context.OtherSideEffectMasters.Count(o =>
                        o.AllopathicDrugId == drug.AllopathicDrugId && o.DeleteStatus == false
                    ),
                    TotalSeriousSideEffects = _context.SeriousSideEffectMasters.Count(s =>
                        s.AllopathicDrugId == drug.AllopathicDrugId && s.DeleteStatus == false
                    ),
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
                .ThenInclude(g => g.DrugSystem)
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
                DrugSystemId = drug.DrugGroup?.DrugSystemId ?? 0,
                DrugSystemName = drug.DrugGroup?.DrugSystem?.DrugSystemName,
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
            model.TotalAdverseReactions = model.AdverseReactionModelList.Count;
            model.TotalOtherSideEffects = model.OtherSideEffectModelList.Count;
            model.TotalSeriousSideEffects = model.SeriousSideEffectModelList.Count;
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
                allopathicDrugEntity.TotalAdverseReactions = adverseReactionEntity.Count;
                allopathicDrugEntity.TotalOtherSideEffects = otherSideEffectEntity.Count;
                allopathicDrugEntity.TotalSeriousSideEffects = seriousSideEffectEntity.Count;
            }
            else
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section not found";
            }
            return allopathicDrugEntity;
        }


        #region Old API compatible overloads
#nullable disable

        public List<AllopathicDrugModel> GetAllopathicDrug(ref ErrorResponseModel errorResponseModel)
        {
            var allopathicDrugModelList = new List<AllopathicDrugModel>();
            errorResponseModel = new ErrorResponseModel();
            var allopathicDrugEntity = _context.AllopathicDrugMasters.AsNoTracking()
                .Include(x => x.DrugGroup).ThenInclude(x => x.DrugSystem)
                .Where(x => x.DeleteStatus == false).ToList();
            if (allopathicDrugEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "AllopathicDrug not found";
            }

            var adverseReactionLookup = _context.AdverseReactionMasters.AsNoTracking().Where(x => x.DeleteStatus == false).ToLookup(x => x.AllopathicDrugId);
            var otherSideEffectLookup = _context.OtherSideEffectMasters.AsNoTracking().Where(x => x.DeleteStatus == false).ToLookup(x => x.AllopathicDrugId);
            var seriousSideEffectLookup = _context.SeriousSideEffectMasters.AsNoTracking().Where(x => x.DeleteStatus == false).ToLookup(x => x.AllopathicDrugId);

            foreach (var item in allopathicDrugEntity)
            {
                allopathicDrugModelList.Add(new AllopathicDrugModel
                {
                    DrugGroupId = item.DrugGroupId,
                    DrugGroupName = item.DrugGroup.DrugGroupName,
                    DrugSystemId = item.DrugGroup.DrugSystemId,
                    DrugSystemName = item.DrugGroup.DrugSystem?.DrugSystemName,
                    AllopathicDrugId = item.AllopathicDrugId,
                    AllopathicDrugName = item.AllopathicDrugName,
                    DeleteStatus = item.DeleteStatus,
                    TotalAdverseReactions = adverseReactionLookup[item.AllopathicDrugId].Count(),
                    TotalOtherSideEffects = otherSideEffectLookup[item.AllopathicDrugId].Count(),
                    TotalSeriousSideEffects = seriousSideEffectLookup[item.AllopathicDrugId].Count(),
                    AdverseReactionModelList = adverseReactionLookup[item.AllopathicDrugId].Select(a => new AdverseReactionModel
                    {
                        AdverseReactionId = (int)a.AdverseReactionId,
                        AllopathicDrugId = a.AllopathicDrugId,
                        AllopathicDrugName = item.AllopathicDrugName,
                        AdverseReactionName = a.AdverseReactionName,
                        DeleteStatus = a.DeleteStatus,
                    }).ToList(),
                    OtherSideEffectModelList = otherSideEffectLookup[item.AllopathicDrugId].Select(o => new OtherSideEffectModel
                    {
                        OtherSideEffectId = (int)o.OtherSideEffectId,
                        AllopathicDrugId = o.AllopathicDrugId,
                        AllopathicDrugName = item.AllopathicDrugName,
                        OtherSideEffectName = o.OtherSideEffectName,
                        DeleteStatus = o.DeleteStatus,
                    }).ToList(),
                    SeriousSideEffectModelList = seriousSideEffectLookup[item.AllopathicDrugId].Select(s => new SeriousSideEffectModel
                    {
                        SeriousSideEffectId = (int)s.SeriousSideEffectId,
                        AllopathicDrugId = s.AllopathicDrugId,
                        AllopathicDrugName = item.AllopathicDrugName,
                        SeriousSideEffectName = s.SeriousSideEffectName,
                        DeleteStatus = s.DeleteStatus,
                    }).ToList(),
                });
            }
            return allopathicDrugModelList;
        }

        /// <summary>
        /// Methood to get AllopathicDrug by adverseReactionId
        /// </summary>
        /// <param name="allopathicDrugId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public AllopathicDrugModel GetAllopathicDrugById(long allopathicDrugId, ref ErrorResponseModel errorResponseModel)
        {
            var listAllopathicDrugModel = new AllopathicDrugModel();
            errorResponseModel = new ErrorResponseModel();
            //if (allopathicDrugId == 0)
            //{
            //    var listSubsectionEntity = _context.SectionMasters.Where(x => x.DeleteStatus == false).ToList();
            //    if (listSubsectionEntity == null)
            //    {
            //        errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            //        errorResponseModel.Message = "Section not found";
            //    }


            //    listAllopathicDrugModel.SubSectionId = 0;
            //    listAllopathicDrugModel.SubSectionName = listAllopathicDrugModel.SectionName;
            //    listAllopathicDrugModel.SectionId = listAllopathicDrugModel.SectionId;
            //    listAllopathicDrugModel.ParentSubSectionId = listAllopathicDrugModel.ParentSubSectionId;


            //}

            //else
            //{
                var listSubsectionEntity = _context.AllopathicDrugMasters.Include(x=>x.DrugGroup).ThenInclude(x => x.DrugSystem).Where(x => x.DeleteStatus == false).Where((x => x.AllopathicDrugId == allopathicDrugId)).FirstOrDefault();

                //Get all recodrs from AdverseReactionMaster join with AllopathicDrugMaster on allopathicDrugId
                var adverseReactionEntity = (from adverseReactionMaster in _context.AdverseReactionMasters
                                                   join allopathicDrug in _context.AllopathicDrugMasters
                                                   on adverseReactionMaster.AllopathicDrugId equals allopathicDrug.AllopathicDrugId
                                                   where allopathicDrug.AllopathicDrugId == allopathicDrugId && adverseReactionMaster.DeleteStatus == false
                                                   select new AdverseReactionModel
                                                   {
                                                       AdverseReactionId = (int)adverseReactionMaster.AdverseReactionId,
                                                       AllopathicDrugId = adverseReactionMaster.AllopathicDrugId,
                                                       AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                                                       AdverseReactionName = adverseReactionMaster.AdverseReactionName,
                                                       DeleteStatus = adverseReactionMaster.DeleteStatus,
                                                   }).ToList();


                var otherSideEffectEntity = (from otherSideEffect in _context.OtherSideEffectMasters
                                             join allopathicDrug in _context.AllopathicDrugMasters
                                             on otherSideEffect.AllopathicDrugId equals allopathicDrug.AllopathicDrugId
                                             where allopathicDrug.AllopathicDrugId == allopathicDrugId && otherSideEffect.DeleteStatus == false
                                             select new OtherSideEffectModel
                                                {
                                                    OtherSideEffectId = (int)otherSideEffect.OtherSideEffectId,
                                                    AllopathicDrugId = otherSideEffect.AllopathicDrugId,
                                                    AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                                                    OtherSideEffectName = otherSideEffect.OtherSideEffectName,
                                                    DeleteStatus = otherSideEffect.DeleteStatus,

                                             }).ToList();

                var seriousSideEffectEntity = (from seriousSideEffect in _context.SeriousSideEffectMasters
                                             join allopathicDrug in _context.AllopathicDrugMasters
                                             on seriousSideEffect.AllopathicDrugId equals allopathicDrug.AllopathicDrugId
                                             where allopathicDrug.AllopathicDrugId == allopathicDrugId && seriousSideEffect.DeleteStatus == false
                                             select new SeriousSideEffectModel
                                             {
                                                 SeriousSideEffectId = (int)seriousSideEffect.SeriousSideEffectId,
                                                 AllopathicDrugId = seriousSideEffect.AllopathicDrugId,
                                                 AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                                                 SeriousSideEffectName = seriousSideEffect.SeriousSideEffectName,
                                                 DeleteStatus = seriousSideEffect.DeleteStatus,

                                             }).ToList();

                if (listSubsectionEntity == null)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Section not found";
                }
                else
                {
                    listAllopathicDrugModel.AllopathicDrugId = listSubsectionEntity.AllopathicDrugId;
                    listAllopathicDrugModel.AllopathicDrugName = listSubsectionEntity.AllopathicDrugName;
                    listAllopathicDrugModel.DrugGroupId = listSubsectionEntity.DrugGroupId;
                    listAllopathicDrugModel.DrugGroupName = listSubsectionEntity.DrugGroup.DrugGroupName;
                    listAllopathicDrugModel.DrugSystemId = listSubsectionEntity.DrugGroup.DrugSystemId;
                    listAllopathicDrugModel.DrugSystemName = listSubsectionEntity.DrugGroup.DrugSystem?.DrugSystemName;
                    listAllopathicDrugModel.DeleteStatus = listSubsectionEntity.DeleteStatus;
                    listAllopathicDrugModel.AdverseReactionModelList = adverseReactionEntity;
                    listAllopathicDrugModel.OtherSideEffectModelList = otherSideEffectEntity;
                    listAllopathicDrugModel.SeriousSideEffectModelList = seriousSideEffectEntity;
                    listAllopathicDrugModel.TotalAdverseReactions = adverseReactionEntity.Count;
                    listAllopathicDrugModel.TotalOtherSideEffects = otherSideEffectEntity.Count;
                    listAllopathicDrugModel.TotalSeriousSideEffects = seriousSideEffectEntity.Count;

                }
            //}

            return listAllopathicDrugModel;
        }

#nullable restore
        #endregion
    }
}
