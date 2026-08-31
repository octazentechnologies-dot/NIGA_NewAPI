using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories
{
    public class ThreeDBodyPartMeshKeyMasterService : IThreeDBodyPartMeshKeyMasterRepository
    {
        private readonly NIGACentrumContext _context;

        public ThreeDBodyPartMeshKeyMasterService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<ThreeDBodyPartMeshKeyMaster?> GetMeshKeyEntityByIdAsync(int meshKeyId)
        {
            return await _context.ThreeDBodyPartMeshKeyMasters
                .FirstOrDefaultAsync(x => x.ThreeDBodyPartMeshKeyId == meshKeyId && !x.DeleteStatus);
        }

        public async Task<ThreeDBodyPartMeshKeyMasterModel?> GetMeshKeyDetailsByIdAsync(int meshKeyId)
        {
            return await (
                from m in _context.ThreeDBodyPartMeshKeyMasters
                where m.ThreeDBodyPartMeshKeyId == meshKeyId && !m.DeleteStatus
                select new ThreeDBodyPartMeshKeyMasterModel
                {
                    ThreeDBodyPartMeshKeyId = m.ThreeDBodyPartMeshKeyId,
                    ThreeDBodyPartMeshKeyName = m.ThreeDBodyPartMeshKeyName,
                    EnteredBy = m.EnteredBy,
                    EnteredDate = m.EnteredDate,
                    ChangedBy = m.ChangedBy,
                    ChangedDate = m.ChangedDate,
                    DeleteStatus = m.DeleteStatus
                }).FirstOrDefaultAsync();
        }

        public async Task<PaginatedResult<ThreeDBodyPartMeshKeyMasterModel>> GetAllMeshKeysAsync(
            PaginationRequestModel request)
        {
            var query = _context.ThreeDBodyPartMeshKeyMasters
                .AsNoTracking()
                .ApplyMeshKeyFilters(request)
                .Select(m => new ThreeDBodyPartMeshKeyMasterModel
                {
                    ThreeDBodyPartMeshKeyId = m.ThreeDBodyPartMeshKeyId,
                    ThreeDBodyPartMeshKeyName = m.ThreeDBodyPartMeshKeyName,
                    EnteredBy = m.EnteredBy,
                    EnteredDate = m.EnteredDate,
                    ChangedBy = m.ChangedBy,
                    ChangedDate = m.ChangedDate,
                    DeleteStatus = m.DeleteStatus
                });

            return await query.ToPaginatedResultAsync(request);
        }

        public async Task<bool> IsDuplicateMeshKeyNameAsync(string meshKeyName, int? excludeMeshKeyId = null)
        {
            var normalizedName = meshKeyName.Trim().ToLower();
            return await _context.ThreeDBodyPartMeshKeyMasters.AnyAsync(x =>
                !x.DeleteStatus
                && x.ThreeDBodyPartMeshKeyName.ToLower() == normalizedName
                && (excludeMeshKeyId == null || x.ThreeDBodyPartMeshKeyId != excludeMeshKeyId));
        }

        public void SaveMeshKey(ThreeDBodyPartMeshKeyMaster meshKey)
        {
            _context.Entry(meshKey).State = EntityState.Added;
        }

        public void UpdateMeshKey(ThreeDBodyPartMeshKeyMaster meshKey)
        {
            _context.Entry(meshKey).State = EntityState.Modified;
        }

        public void DeleteMeshKey(ThreeDBodyPartMeshKeyMaster meshKey)
        {
            meshKey.DeleteStatus = true;
            _context.Entry(meshKey).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
