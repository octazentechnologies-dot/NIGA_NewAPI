using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories
{
    public class ThreeDBodyPartSectionMasterService : IThreeDBodyPartSectionMasterRepository
    {
        private readonly NIGACentrumContext _context;

        public ThreeDBodyPartSectionMasterService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<ThreeDBodyPartSectionMaster?> GetSectionEntityByIdAsync(int sectionMasterId)
        {
            return await _context.ThreeDBodyPartSectionMasters
                .FirstOrDefaultAsync(x => x.ThreeDBodyPartSectionMasterId == sectionMasterId && !x.DeleteStatus);
        }

        public async Task<ThreeDBodyPartSectionMasterModel?> GetSectionDetailsByIdAsync(int sectionMasterId)
        {
            return await BuildSectionDetailsQuery()
                .Where(x => x.ThreeDBodyPartSectionMasterId == sectionMasterId)
                .FirstOrDefaultAsync();
        }

        public async Task<PaginatedResult<ThreeDBodyPartSectionMasterListItem>> GetAllSectionsAsync(
            PaginationRequestModel request)
        {
            return await BuildSectionListQuery(request).ToPaginatedResultAsync(request);
        }

        public async Task<PaginatedResult<ThreeDBodyPartSectionMasterListItem>> GetSectionsByMeshKeyIdAsync(
            int meshKeyId,
            PaginationRequestModel request)
        {
            return await BuildSectionListQuery(request, meshKeyId).ToPaginatedResultAsync(request);
        }

        public async Task<bool> IsDuplicateMappingAsync(
            int meshKeyId,
            int sectionId,
            int? excludeSectionMasterId = null)
        {
            return await _context.ThreeDBodyPartSectionMasters.AnyAsync(x =>
                x.ThreeDBodyPartMeshKeyId == meshKeyId
                && x.ThreeDBodyPartSectionId == sectionId
                && !x.DeleteStatus
                && (excludeSectionMasterId == null || x.ThreeDBodyPartSectionMasterId != excludeSectionMasterId));
        }

        public async Task<bool> MeshKeyExistsAsync(int meshKeyId)
        {
            return await _context.ThreeDBodyPartMeshKeyMasters
                .AnyAsync(x => x.ThreeDBodyPartMeshKeyId == meshKeyId && !x.DeleteStatus);
        }

        public async Task<bool> SectionExistsAsync(int sectionId)
        {
            return await _context.SectionMasters
                .AnyAsync(x => x.SectionId == sectionId && !x.DeleteStatus);
        }

        public async Task<List<ThreeDBodyPartMeshKeyDropdownModel>> GetActiveMeshKeysDropdownAsync()
        {
            return await _context.ThreeDBodyPartMeshKeyMasters
                .AsNoTracking()
                .Where(x => !x.DeleteStatus)
                .OrderBy(x => x.ThreeDBodyPartMeshKeyName)
                .Select(x => new ThreeDBodyPartMeshKeyDropdownModel
                {
                    ThreeDBodyPartMeshKeyId = x.ThreeDBodyPartMeshKeyId,
                    ThreeDBodyPartMeshKeyName = x.ThreeDBodyPartMeshKeyName
                })
                .ToListAsync();
        }

        public async Task<List<SectionMasterDropdownModel>> GetActiveSectionsDropdownAsync()
        {
            return await _context.SectionMasters
                .AsNoTracking()
                .Where(x => !x.DeleteStatus)
                .OrderBy(x => x.SectionName)
                .Select(x => new SectionMasterDropdownModel
                {
                    SectionId = x.SectionId,
                    SectionName = x.SectionName
                })
                .ToListAsync();
        }

        public void SaveSection(ThreeDBodyPartSectionMaster section)
        {
            _context.Entry(section).State = EntityState.Added;
        }

        public void UpdateSection(ThreeDBodyPartSectionMaster section)
        {
            _context.Entry(section).State = EntityState.Modified;
        }

        public void DeleteSection(ThreeDBodyPartSectionMaster section)
        {
            section.DeleteStatus = true;
            _context.Entry(section).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        private IQueryable<ThreeDBodyPartSectionMasterModel> BuildSectionDetailsQuery()
        {
            return from s in _context.ThreeDBodyPartSectionMasters.AsNoTracking()
                   join m in _context.ThreeDBodyPartMeshKeyMasters
                       on s.ThreeDBodyPartMeshKeyId equals m.ThreeDBodyPartMeshKeyId into meshKeys
                   from m in meshKeys.DefaultIfEmpty()
                   join sec in _context.SectionMasters
                       on s.ThreeDBodyPartSectionId equals sec.SectionId into sections
                   from sec in sections.DefaultIfEmpty()
                   where !s.DeleteStatus
                   select new ThreeDBodyPartSectionMasterModel
                   {
                       ThreeDBodyPartSectionMasterId = s.ThreeDBodyPartSectionMasterId,
                       ThreeDBodyPartMeshKeyId = s.ThreeDBodyPartMeshKeyId,
                       ThreeDBodyPartMeshKeyName = m != null ? m.ThreeDBodyPartMeshKeyName : null,
                       ThreeDBodyPartSectionId = s.ThreeDBodyPartSectionId,
                       SectionName = sec != null ? sec.SectionName : null,
                       EnteredBy = s.EnteredBy,
                       EnteredDate = s.EnteredDate,
                       ChangedBy = s.ChangedBy,
                       ChangedDate = s.ChangedDate,
                       DeleteStatus = s.DeleteStatus
                   };
        }

        private IQueryable<ThreeDBodyPartSectionMasterListItem> BuildSectionListQuery(
            PaginationRequestModel request,
            int? meshKeyId = null)
        {
            return _context.ThreeDBodyPartSectionMasters
                .AsNoTracking()
                .Include(s => s.ThreeDBodyPartMeshKey)
                .Include(s => s.Section)
                .ApplySectionMasterFilters(request, meshKeyId)
                .Select(s => new ThreeDBodyPartSectionMasterListItem
                {
                    ThreeDBodyPartSectionMasterId = s.ThreeDBodyPartSectionMasterId,
                    ThreeDBodyPartMeshKeyId = s.ThreeDBodyPartMeshKeyId,
                    ThreeDBodyPartMeshKeyName = s.ThreeDBodyPartMeshKey != null
                        ? s.ThreeDBodyPartMeshKey.ThreeDBodyPartMeshKeyName
                        : null,
                    ThreeDBodyPartSectionId = s.ThreeDBodyPartSectionId,
                    SectionName = s.Section != null ? s.Section.SectionName : null
                });
        }
    }
}
