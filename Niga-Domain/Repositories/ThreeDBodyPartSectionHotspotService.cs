using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories
{
    public class ThreeDBodyPartSectionHotspotService : IThreeDBodyPartSectionHotspotRepository
    {
        private readonly NIGACentrumContext _context;

        public ThreeDBodyPartSectionHotspotService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<ThreeDBodyPartSectionHotspot?> GetHotspotEntityByIdAsync(int sectionHotspotId)
        {
            return await _context.ThreeDBodyPartSectionHotspots
                .FirstOrDefaultAsync(x => x.SectionHotspotId == sectionHotspotId && !x.DeleteStatus);
        }

        public async Task<ThreeDBodyPartSectionHotspotModel?> GetHotspotDetailsByIdAsync(int sectionHotspotId)
        {
            return await (
                from h in _context.ThreeDBodyPartSectionHotspots
                join sec in _context.SectionMasters
                    on h.SectionId equals sec.SectionId into sections
                from sec in sections.DefaultIfEmpty()
                where h.SectionHotspotId == sectionHotspotId && !h.DeleteStatus
                select new ThreeDBodyPartSectionHotspotModel
                {
                    SectionHotspotId = h.SectionHotspotId,
                    SectionId = h.SectionId,
                    SectionName = sec != null ? sec.SectionName : null,
                    HotspotName = h.HotspotName,
                    EnteredBy = h.EnteredBy,
                    EnteredDate = h.EnteredDate,
                    ChangedBy = h.ChangedBy,
                    ChangedDate = h.ChangedDate,
                    DeleteStatus = h.DeleteStatus
                }).FirstOrDefaultAsync();
        }

        public async Task<PaginatedResult<ThreeDBodyPartSectionHotspotListItem>> GetAllHotspotsAsync(
            PaginationRequestModel request)
        {
            return await BuildHotspotListQuery(request).ToPaginatedResultAsync(request);
        }

        public async Task<PaginatedResult<ThreeDBodyPartSectionHotspotListItem>> GetHotspotsBySectionIdAsync(
            int sectionId,
            PaginationRequestModel request)
        {
            return await BuildHotspotListQuery(request, sectionId).ToPaginatedResultAsync(request);
        }

        public async Task<bool> IsDuplicateHotspotNameAsync(int sectionId, string hotspotName, int? excludeSectionHotspotId = null)
        {
            var normalizedName = hotspotName.Trim().ToLower();
            return await _context.ThreeDBodyPartSectionHotspots.AnyAsync(x =>
                x.SectionId == sectionId
                && !x.DeleteStatus
                && x.HotspotName.ToLower() == normalizedName
                && (excludeSectionHotspotId == null || x.SectionHotspotId != excludeSectionHotspotId));
        }

        public async Task<bool> SectionExistsAsync(int sectionId)
        {
            return await _context.SectionMasters.AnyAsync(x => x.SectionId == sectionId && !x.DeleteStatus);
        }

        public void SaveHotspot(ThreeDBodyPartSectionHotspot hotspot)
        {
            _context.Entry(hotspot).State = EntityState.Added;
        }

        public void UpdateHotspot(ThreeDBodyPartSectionHotspot hotspot)
        {
            _context.Entry(hotspot).State = EntityState.Modified;
        }

        public void DeleteHotspot(ThreeDBodyPartSectionHotspot hotspot)
        {
            hotspot.DeleteStatus = true;
            _context.Entry(hotspot).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        private IQueryable<ThreeDBodyPartSectionHotspotListItem> BuildHotspotListQuery(
            PaginationRequestModel request,
            int? sectionId = null)
        {
            var filteredHotspots = _context.ThreeDBodyPartSectionHotspots
                .AsNoTracking()
                .ApplyHotspotFilters(request, sectionId);

            return from h in filteredHotspots
                   join sec in _context.SectionMasters
                       on h.SectionId equals sec.SectionId into sections
                   from sec in sections.DefaultIfEmpty()
                   select new ThreeDBodyPartSectionHotspotListItem
                   {
                       SectionHotspotId = h.SectionHotspotId,
                       SectionId = h.SectionId,
                       SectionName = sec != null ? sec.SectionName : null,
                       HotspotName = h.HotspotName
                   };
        }
    }
}
