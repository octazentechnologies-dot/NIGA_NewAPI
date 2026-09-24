using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;


namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    /// <summary>
    /// Interface used for master related operations
    /// </summary>
    public interface IMastersAPIService
    {
        /// <summary>
        /// Method is used for get all the states
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<StateModel> GetStates(int? CountryId,ref ErrorResponseModel errorResponseModel);
        
        /// <summary>
        /// Method is used for get all the countries
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<CountryModel> GetCountries(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Genders
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<GenderModel> GetGenders(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Packages
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<PackageModel> GetPackages(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Qualifications
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<QualificationModel> GetQualifications(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all active languages
        /// </summary>
        List<LanguageMasterModel> GetLanguages(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Diagnosis Groups
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<DiagnosisGroupModel> GetDiagnosisGroups(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Diagnosis
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<DiagnosisModel> GetDiagnosis(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Sections
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<SectionModel> GetSections(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the SubSections
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<SubSectionModel> GetSubSections(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the SubSections by section id
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<SubSectionModel> GetSubsectionBySection (long sectionId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Remedies
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<RemedyModel> GetRemedies(RubricRemedyDetailsModel rubricRemedyDetailsModel,ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Intensities
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<IntensityModel> GetIntensities(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Remedy Grades
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<RemedyGradeModel> GetRemedyGrades(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// interface for getting all the bodyparts
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<BodyPartModel> GetBodyParts(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// interface for getting all the partlocations
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<PartLocationModel> GetPartLocations(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// interface for getting all the questionsections
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<QuestionSectionModel> GetQuestionSections(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method declaration for getting all the Chief Complaints
        /// </summary>
        /// <returns></returns>
        List<CaseEntryChiefComplaintModel> getAllChiefComplaints(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Clinical Questions
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<ClinicalQuestionsModel> GetClinicalQuestions(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Question Group
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<QuestionGroupModel> GetQuestionGroup(ref ErrorResponseModel errorResponseModel);


        /// <summary>
        /// Method is used for get all the subsection by bodypartId
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<SubSectionModel> GetSubSectionByBodyPart(long bodyPartId,ref ErrorResponseModel errorResponseModel);


        /// <summary>
        /// Method is used for get all the subsection by bodypartId
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<SubSectionModel> GetSubSectionByBodyPart(string subSectionName, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used to get all doctor list
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<DoctorModel> GetDoctorList(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Module Master
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<ModuleMasterModel> GetModuleMaster(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Firm Details
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<FirmDetailsModel> GetFirmDetails(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// M02 W7 / ADM-B04 — menus allowed for the user's RoleId (from RoleDetails + MenuMaster).
        /// jwtRoleName is required for Reception: staff NameIdentifier is not UserMaster, and
        /// the doctor UserId on the JWT must not inherit Doctor case-taking menus.
        /// </summary>
        List<MenuMasterModel> GetMenuByRole(long userId, ref ErrorResponseModel errorResponseModel);

        List<MenuMasterModel> GetMenuByRole(long userId, string jwtRoleName, bool inspectOtherUser, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get doctor by user Id
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<DoctorModel> GetDoctorById(long userId, ref ErrorResponseModel errorResponseModel);

        // PaginationResult GetSubSectionBySearch(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);

        // PaginationResult GetSubsectionBySectionWithPagination(int sectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
    }
}
