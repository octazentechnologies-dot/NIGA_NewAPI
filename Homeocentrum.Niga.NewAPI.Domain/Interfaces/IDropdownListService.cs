using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    public interface IDropdownListService
    {
        List<ThermalModel> GetAllThermalDDL(string? Desc);
        List<AuthorMasterModel> GetAuthorforMateriaMedica(string? Desc);
        List<PatientLabTestModel> GetPatientLabTestDDl(string? Desc);

        List<QuestionGroupModelDDL> GetQuestionGroupDDL(string? Desc);

        List<QuestionSectionModelDDL> GetQuestionSectionsDDL(string? Desc);

        List<QuestionSubGroupModelDDL> GetQuestionSubGroupDDL(string? Desc);

        List<BodyPartDDLModel> GetBodyPartDDL(int sectionId, string? Desc);

        List<QuestionSubGroupModelDDL> GetSubQuestionGroupByQGIDQSIDDDL(int questionGroupId, int questionSectionId, string? Desc);

        List<SubSectionDDLModel> GetSubsectionBySection(long sectionId, string? Desc);

       
    }
}
