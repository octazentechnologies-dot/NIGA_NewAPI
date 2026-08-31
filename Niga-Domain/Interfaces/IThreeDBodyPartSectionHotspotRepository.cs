using System.Collections.Generic;
using System.Threading.Tasks;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
{
    /// <summary>
    /// Interface for ThreeDBodyPartSectionHotspot operations
    /// </summary>
    public interface IThreeDBodyPartSectionHotspotRepository
    {
        Task<ThreeDBodyPartSectionHotspot?> GetHotspotEntityByIdAsync(int sectionHotspotId);

        Task<ThreeDBodyPartSectionHotspotModel?> GetHotspotDetailsByIdAsync(int sectionHotspotId);

        Task<PaginatedResult<ThreeDBodyPartSectionHotspotListItem>> GetAllHotspotsAsync(PaginationRequestModel request);

        Task<PaginatedResult<ThreeDBodyPartSectionHotspotListItem>> GetHotspotsBySectionIdAsync(int sectionId, PaginationRequestModel request);

        Task<bool> IsDuplicateHotspotNameAsync(int sectionId, string hotspotName, int? excludeSectionHotspotId = null);

        Task<bool> SectionExistsAsync(int sectionId);

        void SaveHotspot(ThreeDBodyPartSectionHotspot hotspot);

        void UpdateHotspot(ThreeDBodyPartSectionHotspot hotspot);

        void DeleteHotspot(ThreeDBodyPartSectionHotspot hotspot);

        Task<bool> SaveAllAsync();
    }
}
