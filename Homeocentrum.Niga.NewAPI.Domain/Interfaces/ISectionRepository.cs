using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    
    public interface ISectionRepository
    {
        Task<SectionMaster> GetSectionById(long sectionId);
        Task<PagedList<SectionList>> getAllSections(ParameterParams parameterParams);
        List<SectionModel> getAllRemedyByFilter(string search, int SectionId, ref ErrorResponseModel errorResponseModel);
        void SaveSection(SectionMaster section);
        void UpdateSection(SectionMaster section);
        void DeleteSection(SectionMaster section);
        Task<SectionMasterDto> GetSectionDetailsById(long sectionId);
        Task<bool> SaveAllAsync();
        Task<List<SectionMasterDto>> GetSectionDD(string? Search);

    }
}
