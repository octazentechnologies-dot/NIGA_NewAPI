using API.Helpers;
using Microsoft.AspNetCore.Http;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Niga_Domain.Interfaces
{
    /// <summary>
    /// Interface used for subsection related operations
    /// </summary>
    public interface ISubSectionRepository
    {
        Task<AddSubSectionModel> GetSubSectionById(long subsectionId);
        Task<PagedList<SubSectionList>> GetSubSectionList(ParameterParams parameter);
        Task<RubricKeywordSearchPagedResponse> SearchRubricsByKeywordAsync(string keyword, int pageNumber, int pageSize, List<int> sectionIds = null);
        Task<PaginatedResult<SubSectionForPageModel>> SearchSubSectionsByHotspotAsync(SearchSubSectionByHotspotRequest request);
        // List<SectionModel> GetSubSectionsBySection(SectionModel sectionModel);
       // List<SubSectionModel> GetSubSections(int sectionId,NigaParameters nigaParameters);
        List<SubSection> GetSubSectionsByDate(int userId, ref ErrorResponseModel errorResponseModel);
        // string DeleteSubSectionLanguageDetail(SubSectionLanguageDetailModel subSectionLanguageDetailsModel, ref ErrorResponseModel errorResponseModel);
        // string DeleteSubSectionLanguageDetail(ReferenceRubricDetailModel referenceRubricDetailsModel, ref ErrorResponseModel errorResponseModel);
        // PaginationResult GetSubSectionsWithPagination(int sectionId, NigaParameters nigaParameters);
          void SaveSubSection(SubSectionMaster subSection);
          void SaveReferenceRubric(ReferenceRubricDetail rubricDetails);
          void SaveSubsectionlanguage(SubSectionLanguageDetail languageDetails);
          Task<SubSectionMaster> GetSubSectionById(int SubSectionId);  
          Task<ReferenceRubricDetail> GetReferenceRubricById(int ReferenceRubricId);
          Task<SubSectionLanguageDetail> GetSubLanguageById(int SubSectionLanguageId);
        void UpdateSubSection(SubSectionMaster subSection);
        void UpdateReferenceRubric(ReferenceRubricDetail rubricDetails);
          void UpdateSubsectionlanguage(SubSectionLanguageDetail languageDetails);

        void DeleteSubSection(SubSectionMaster subSection);
        void DeleteLanguageDetails(SubSectionLanguageDetail languageDetails);
        void DeleterubricDetails(ReferenceRubricDetail rubricDetails);
        Task<bool> SaveAllAsync();
       // Task<List<SubSectionDDModel>> GetSectionDD(string? Search);
        Task<(bool Success, string Message, int TotalRows, int SuccessRows, int FailedRows)> ImportSubSectionsFromExcel(IFormFile file);
        Task<byte[]> ExportSubSectionsToExcel(int sectionId);
        Task<(bool Success, string Message, int TotalRows, int SuccessRows, int FailedRows)> UpdateSubSectionsFromExcel(IFormFile file);
        Task<(bool Success, string Message, int TotalRows, int SuccessRows, int FailedRows)> ImportSubSectionsFromExcelPC(IFormFile file);
        byte[] GetReferenceRubricsImportTemplate(string format);
        Task<ReferenceRubricImportResultModel> ImportReferenceRubricsAsync(IFormFile file);
    }





}
