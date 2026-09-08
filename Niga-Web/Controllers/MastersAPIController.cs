using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Business.Interface;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for master entity 
    /// </summary>
    [Route("api/mastersAPI")]
    [ApiController]
    [Authorize]
    public class MastersAPIController : BaseAPIController
    {
        IMastersAPIService _mastersAPIService;
        /// <summary>
        /// Used to initialize controller and inject master service
        /// </summary>
        /// <param name="mastersAPIService"></param>
        public MastersAPIController(IMastersAPIService mastersAPIService)
        {
            _mastersAPIService = mastersAPIService;
        }

        /// <summary>
        /// To get all states
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet ("GetStates")]
        [ProducesResponseType(typeof(StateModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetStates(int? CountryId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var stateModel = _mastersAPIService.GetStates(CountryId,ref errorResponseModel);

                if (stateModel != null)
                {
                    return Ok(stateModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        /// <summary>
        /// To get all countries
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetCountries")]
        [ProducesResponseType(typeof(CountryModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetCountries()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var countryModel = _mastersAPIService.GetCountries(ref errorResponseModel);

                if (countryModel != null)
                {
                    return Ok(countryModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

       
        /// <summary>
        /// To get all genders
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        /// <summary>
        /// To get all active languages (e.g. English, Marathi for WhatsApp templates).
        /// </summary>
        [HttpGet("GetLanguages")]
        [ProducesResponseType(typeof(LanguageMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetLanguages()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var languageModelList = _mastersAPIService.GetLanguages(ref errorResponseModel);

                if (languageModelList != null)
                {
                    return Ok(languageModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet ("GetGenders")]
        [ProducesResponseType(typeof(GenderModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetGenders()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var genderModelList = _mastersAPIService.GetGenders(ref errorResponseModel);

                if (genderModelList != null)
                {
                    return Ok(genderModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all packages
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetPackages")]
        [ProducesResponseType(typeof(PackageModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPackages()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var packageModelList = _mastersAPIService.GetPackages(ref errorResponseModel);

                if (packageModelList != null)
                {
                    return Ok(packageModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        /// To get all qualifications
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetQualifications")]
        [ProducesResponseType(typeof(QualificationModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetQualifications()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var qualificationModelList = _mastersAPIService.GetQualifications(ref errorResponseModel);

                if (qualificationModelList != null)
                {
                    return Ok(qualificationModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all diagnosisgroups
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetDiagnosisGroups")]
        [ProducesResponseType(typeof(DiagnosisGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosisGroups()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisgroupModelList = _mastersAPIService.GetDiagnosisGroups(ref errorResponseModel);

                if (diagnosisgroupModelList != null)
                {
                    return Ok(diagnosisgroupModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all diagnosis
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetDiagnosis")]
        [ProducesResponseType(typeof(DiagnosisModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosis()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisModelList = _mastersAPIService.GetDiagnosis(ref errorResponseModel);

                if (diagnosisModelList != null)
                {
                    return Ok(diagnosisModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all sections
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSections")]
        [ProducesResponseType(typeof(SectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSections()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var sectionModelList = _mastersAPIService.GetSections(ref errorResponseModel);

                if (sectionModelList != null)
                {
                    return Ok(sectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all subsections
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubSections")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubSections()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModelList = _mastersAPIService.GetSubSections(ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        /// <summary>
        /// To get all subsections
        /// </summary>
        /// <param name="sectionId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubsectionBySection/{sectionId}")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubsectionBySection(long sectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModelList = _mastersAPIService.GetSubsectionBySection(sectionId, ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all remedies
        /// </summary>
        /// <param name="rubricRemedyDetailsModel"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("GetRemedies")]
        [ProducesResponseType(typeof(RemedyModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRemedies(RubricRemedyDetailsModel rubricRemedyDetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModelList = _mastersAPIService.GetRemedies(rubricRemedyDetailsModel,ref errorResponseModel);

                if (remedyModelList != null)
                {
                    return Ok(remedyModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all intensities
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetIntensities")]
        [ProducesResponseType(typeof(IntensityModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetIntensities()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var intensityModelList = _mastersAPIService.GetIntensities(ref errorResponseModel);

                if (intensityModelList != null)
                {
                    return Ok(intensityModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all remedygrades
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRemedyGrades")]
        [ProducesResponseType(typeof(RemedyGradeModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRemedyGrades()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedygradeModelList = _mastersAPIService.GetRemedyGrades(ref errorResponseModel);

                if (remedygradeModelList != null)
                {
                    return Ok(remedygradeModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To Get all bodyparts
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetBodyParts")]
        [ProducesResponseType(typeof(BodyPartModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetBodyParts()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var bodypartModelList = _mastersAPIService.GetBodyParts(ref errorResponseModel);

                if (bodypartModelList != null)
                {
                    return Ok(bodypartModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all partlocations
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetPartLocations")]
        [ProducesResponseType(typeof(PartLocationModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPartLocations()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var partlocationModelList = _mastersAPIService.GetPartLocations(ref errorResponseModel);

                if (partlocationModelList != null)
                {
                    return Ok(partlocationModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To Get all questionsections
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionSection")]
        [ProducesResponseType(typeof(QuestionSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetQuestionSections()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionsectionModelList = _mastersAPIService.GetQuestionSections(ref errorResponseModel);

                if (questionsectionModelList != null)
                {
                    return Ok(questionsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all Chief Complaints
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("getAllChiefComplaints")]
        [ProducesResponseType(typeof(CaseEntryChiefComplaintModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult getAllChiefComplaints()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var caseEntryChiefComplaintModellist = _mastersAPIService.getAllChiefComplaints(ref errorResponseModel);

                if (caseEntryChiefComplaintModellist.Count > 0)
                {
                    return Ok(caseEntryChiefComplaintModellist);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all clinical questions
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetClinicalQuestions")]
        [ProducesResponseType(typeof(ClinicalQuestionsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClinicalQuestions()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModelList = _mastersAPIService.GetClinicalQuestions(ref errorResponseModel);

                if (clinicalquestionModelList != null)
                {
                    return Ok(clinicalquestionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all questiongroup
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetQuestionGroup")]
        [ProducesResponseType(typeof(QuestionGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetQuestionGroup()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questiongroupModelList = _mastersAPIService.GetQuestionGroup(ref errorResponseModel);

                if (questiongroupModelList != null)
                {
                    return Ok(questiongroupModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all subsections
        /// </summary>
        /// <param name="sectionId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubSectionByBodyPart/{bodyPartId}")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubSectionByBodyPart(long bodyPartId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModelList = _mastersAPIService.GetSubSectionByBodyPart(bodyPartId, ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all subsections
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("SearchSubSections/{keyword}")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SearchSubSections(string keyword)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModelList = _mastersAPIService.GetSubSectionByBodyPart(keyword, ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all states
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetDoctors")]
        [ProducesResponseType(typeof(DoctorModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDoctors()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var doctorModelList = _mastersAPIService.GetDoctorList(ref errorResponseModel);

                if (doctorModelList != null)
                {
                    return Ok(doctorModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all module master
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetModuleMaster")]
        [ProducesResponseType(typeof(ModuleMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetModuleMaster()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var moduleMasterModelList = _mastersAPIService.GetModuleMaster(ref errorResponseModel);

                if (moduleMasterModelList != null)
                {
                    return Ok(moduleMasterModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all firm Details
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetFirmDetails")]
        [ProducesResponseType(typeof(FirmDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetFirmDetails()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var firmDetailsModelList = _mastersAPIService.GetFirmDetails(ref errorResponseModel);

                if (firmDetailsModelList != null)
                {
                    return Ok(firmDetailsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        /// M02 W7 ADM-B04 — Get menus for the role of the given user (New-API restore).
        /// SEC-04.01 / IDOR harden: non-AdminPortal callers are forced to their own JWT userId.
        /// </summary>
        [HttpGet("GetMenuByRole")]
        [ProducesResponseType(typeof(MenuMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMenuByRole(long userId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                if (!User.IsAdminPortalUser())
                {
                    userId = User.GetUserId();
                }

                var menuList = _mastersAPIService.GetMenuByRole(userId, ref errorResponseModel);

                if (menuList != null && menuList.Count > 0)
                {
                    return Ok(menuList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get doctor 
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetDoctorDetails/{userId}")]
        [ProducesResponseType(typeof(DoctorModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDoctorDetails(long userId, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var doctorModelList = _mastersAPIService.GetDoctorById(userId, ref errorResponseModel);

                if (doctorModelList != null)
                {
                    return Ok(doctorModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        // /// <summary>
        // /// To get all subsections
        // /// </summary>
        // /// <param name="keyword"></param>
        // /// <param name=""></param>
        // /// <returns></returns>
        // [HttpGet("SubSectionsBySearch/{keyword}")]
        // [ProducesResponseType(typeof(PaginationResult), 200)]
        // [ProducesResponseType(typeof(string), 404)]
        // [ProducesResponseType(typeof(string), 400)]
        // [ProducesResponseType(typeof(string), 500)]
        // public IActionResult GetSubSectionBySearch(string keyword, [FromQuery] NigaParameters nigaParameters)
        // {
        //     ErrorResponseModel errorResponseModel = null;
        //     try
        //     {
        //         var subsectionModelList = _mastersAPIService.GetSubSectionBySearch(keyword,nigaParameters, ref errorResponseModel);

        //         if (subsectionModelList != null)
        //         {
        //             return Ok(subsectionModelList);
        //         }
        //         return ReturnErrorResponse(errorResponseModel);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

        // /// <summary>
        // /// To get all subsections
        // /// </summary>
        // /// <param name="keyword"></param>
        // /// <param name=""></param>
        // /// <returns></returns>
        // [HttpGet("SubsectionBySectionWithPagination/{sectionId}/{keyword}")]
        // [ProducesResponseType(typeof(PaginationResult), 200)]
        // [ProducesResponseType(typeof(string), 404)]
        // [ProducesResponseType(typeof(string), 400)]
        // [ProducesResponseType(typeof(string), 500)]
        // public IActionResult GetSubsectionBySectionWithPagination(int sectionId, string keyword, [FromQuery] NigaParameters nigaParameters)
        // {
        //     ErrorResponseModel errorResponseModel = null;
        //     try
        //     {
        //         var subsectionModelList = _mastersAPIService.GetSubsectionBySectionWithPagination(sectionId,keyword, nigaParameters, ref errorResponseModel);

        //         if (subsectionModelList != null)
        //         {
        //             return Ok(subsectionModelList);
        //         }
        //         return ReturnErrorResponse(errorResponseModel);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

    }
}
