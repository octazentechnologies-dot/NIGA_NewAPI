using System.Net;
using API.Helpers;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interface;
using Niga_Domain.Master;

namespace Niga_Domain.Services
{
    /// <summary>
    /// Service implementation for remedy  related operations
    /// </summary>
    public class RemedyService : IRemedyService
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor for RemedyService
        /// </summary>
        /// <param name="_context">Database _context</param>
        /// <param name="mapper">AutoMapper instance</param>
        public RemedyService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets a remedy  by its RemedyId
        /// </summary>
        /// <param name="remedyId">RemedyId of the remedy  to retrieve</param>
        /// <returns>Remedy  model if found, null otherwise</returns>
        public async Task<RemedyMaster> GetRemedyById(long remedyId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var r = await _context.RemedyMasters.FirstOrDefaultAsync(x =>
                x.RemedyId == remedyId && !x.DeleteStatus
            );
            if (r == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy  not found";
            }
            return r;
        }

        /// <summary>
        /// Gets all remedy s
        /// </summary>
        /// <param name="parameter">Parameter for filtering and paging</param>
        /// <returns>Paged list of remedy  models</returns>
        public async Task<PagedList<RemedyModel>> GetAllRemedys(ParameterParams parameter)
        {
            var remedyModelQuery = (
                from x in _context.RemedyMasters
                select new RemedyModel
                {
                    RemedyId = x.RemedyId,
                    RemedyName = x.RemedyName,
                    RemedyAlias = x.RemedyAlias,
                    Description = x.Description,
                    EnteredBy = x.EnteredBy,
                    EnteredDate = x.EnteredDate,
                    ChangedBy = x.ChangedBy,
                    ChangedDate = x.ChangedDate,
                    DeleteStatus = x.DeleteStatus,
                    ThermalId = x.ThermalId,
                    CommonOrUncommon = x.CommonOrUncommon,
                    ThemesOrCharacteristics = x.ThemesOrCharacteristics,
                    Generals = x.Generals,
                    Modalities = x.Modalities,
                    Particulars = x.Particulars,
                }
            ).AsQueryable();

            if (!string.IsNullOrEmpty(parameter.search))
            {
                remedyModelQuery = remedyModelQuery.Where(x =>
                    x.RemedyAlias.ToString().Contains(parameter.search)
                );
            }

            return await PagedList<RemedyModel>.CreateAsync(
                remedyModelQuery.AsNoTracking(),
                parameter.PageNumber,
                parameter.PageSize
            );
        }

        /// <summary>
        /// Gets all remedy s by filter
        /// </summary>
        /// <param name="search">Search string</param>
        /// <param name="errorResponseModel">Error response model to be populated if any error occurs</param>
        /// <returns>List of remedy  models</returns>
        /// <summary>
        /// Saves or updates a remedy
        /// </summary>
        /// <param name="remedy">Remedy  entity to save or update</param>
        public void SaveRemedy(RemedyMaster remedy)
        {
            _context.Entry(remedy).State = EntityState.Added;
        }

        /// <summary>
        /// Updates a remedy
        /// </summary>
        /// <param name="remedy">Remedy  entity to update</param>
        public void UpdateRemedy(RemedyMaster remedy)
        {
            _context.Entry(remedy).State = EntityState.Modified;
        }

        /// <summary>
        /// Deactivates a remedy
        /// </summary>
        /// <param name="remedy">Remedy  entity to delete</param>
        public void DeleteRemedy(RemedyMaster remedy)
        {
            remedy.DeleteStatus = true;
            _context.Entry(remedy).State = EntityState.Modified;
        }

        /// <summary>
        /// Gets the details of a remedy
        /// </summary>
        /// <param name="remedyId">RemedyId of the remedy  to retrieve</param>
        /// <returns>Remedy  model if found, null otherwise</returns>
        public async Task<RemedyModel> GetRemedyDetailsById(long remedyId)
        {
            var remedyDetails = await (
                from r in _context.RemedyMasters
                where r.RemedyId == remedyId && r.DeleteStatus == false
                select new RemedyModel
                {
                    RemedyId = r.RemedyId,
                    RemedyName = r.RemedyName,
                    RemedyAlias = r.RemedyAlias,
                    Description = r.Description,
                    DeleteStatus = r.DeleteStatus,
                    ThermalId = r.ThermalId,
                    CommonOrUncommon = r.CommonOrUncommon,
                    ThemesOrCharacteristics = r.ThemesOrCharacteristics,
                    Generals = r.Generals,
                    Modalities = r.Modalities,
                    Particulars = r.Particulars,
                }
            ).FirstOrDefaultAsync();
            return remedyDetails;
        }

        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        /// <returns>True if changes are saved successfully, false otherwise</returns>
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public RemedyCommonUncommonModel GetCommonUnCommonRemedyBySection(
            long subSectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            RemedyCommonUncommonModel remedyCommonUncommon = new RemedyCommonUncommonModel();
            errorResponseModel = new ErrorResponseModel();
            var remedyEntities = (
                from remedyDetails in _context.RubricRemedyDetails
                join remedyMaster in _context.RemedyMasters
                    on remedyDetails.RemedyId equals remedyMaster.RemedyId
                join gradeMaster in _context.RemedyGradeMaster
                    on remedyDetails.GradeId equals gradeMaster.GradeId
                where remedyDetails.SubSectionId == subSectionId
                select new RemediesModel
                {
                    RemedyId = Convert.ToInt32(remedyMaster.RemedyId),
                    RemedyName = remedyMaster.RemedyName,
                    RemedyAlias = remedyMaster.RemedyAlias,
                    FontName = gradeMaster.FontName,
                    FontStyle = gradeMaster.FontStyle,
                    FontColor = gradeMaster.FontColor,
                    GradeNo = gradeMaster.GradeNo,
                    ThermalId = remedyMaster.ThermalId,
                    CommonOrUncommon = remedyMaster.CommonOrUncommon,
                }
            ).Distinct().ToList();
            if (remedyEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy not found";
            }
            remedyCommonUncommon.CommonRemedies = remedyEntities
                .Where(x => x.CommonOrUncommon == false)
                .ToList();
            remedyCommonUncommon.UnCommonRemedies = remedyEntities
                .Where(x => x.CommonOrUncommon == true)
                .ToList();

            return remedyCommonUncommon;
        }

        /// <summary>
        /// Imports remedies from an Excel file
        /// </summary>
        /// <param name="file">The Excel file to import</param>
        /// <returns>Result of the import operation</returns>
        public async Task<RemedyImportModel> ImportRemediesFromExcel(IFormFile file)
        {
            var result = new RemedyImportModel();

            try
            {
                if (file == null || file.Length == 0)
                    throw new Exception("No file uploaded");

                if (
                    !Path.GetExtension(file.FileName)
                        .Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                )
                    throw new Exception(
                        "Invalid file format. Please upload an Excel (.xlsx) file."
                    );

                // Create Remedy Excels directory if it doesn't exist
                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "RemedyExcels"
                );
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                string uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Process the saved file
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed();

                // Skip header row
                var dataRows = rows.Skip(1);
                result.TotalRecords = dataRows.Count();

                foreach (var row in dataRows)
                {
                    try
                    {
                        var remedy = new RemedyMaster
                        {
                            RemedyName = row.Cell(1).GetString(),
                            RemedyAlias = row.Cell(2).GetString(),
                            Description = row.Cell(3).GetString(),
                            ThermalId = row.Cell(4).GetValue<int?>(),
                            CommonOrUncommon = row.Cell(5).GetValue<bool?>(),
                            ThemesOrCharacteristics = row.Cell(6).GetString(),
                            Generals = row.Cell(7).GetString(),
                            Modalities = row.Cell(8).GetString(),
                            Particulars = row.Cell(9).GetString(),
                            DeleteStatus = false,
                        };

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(remedy.RemedyName))
                        {
                            throw new Exception($"RemedyName is required at row {row.RowNumber()}");
                        }

                        // Check for duplicate remedy name
                        if (
                            await _context.RemedyMasters.AnyAsync(r =>
                                r.RemedyName == remedy.RemedyName && !r.DeleteStatus
                            )
                        )
                        {
                            throw new Exception(
                                $"Remedy with name '{remedy.RemedyName}' already exists at row {row.RowNumber()}"
                            );
                        }

                        _context.RemedyMasters.Add(remedy);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Error at row {row.RowNumber()}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Import failed: {ex.Message}");
                result.FailureCount = result.TotalRecords;
            }

            return result;
        }
    }
}
