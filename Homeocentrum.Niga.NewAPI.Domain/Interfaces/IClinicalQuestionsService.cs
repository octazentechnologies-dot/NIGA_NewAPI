#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    public interface IClinicalQuestionsService
    {
        /// <summary>
        /// Method is used for to get clinical questions by questionsId
        /// </summary>
        /// <param name="questionsId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        ClinicalQuestionsModel GetClinicalQuestionsById(long questionsId, ref ErrorResponseModel errorResponseModel);
        string AddEditClinicalQuestionsBodyPart(ClinicalQuestionsBodyPartModel clinicalQuestionsBodyPart, ref ErrorResponseModel errorResponseModel);
        List<QuestionKeyWordBodyPartOutputModel> GetClinicalQuestionsKeyWordBodyPart(QuestionKeyWordBodyPartInputModel questionKeyWordBodyPartInput, ref ErrorResponseModel errorResponseModel);
        List<QuestionKeyWordBodyPartRubricOutputModel> GetClinicalRubricData(QuestionKeyWordBodyPartRubricInputModel questionKeyWordBodyPartRubricInput, ref ErrorResponseModel errorResponseModel);
        string DeleteClinicalQuestionBodyPart(int questionId, int userId, ref ErrorResponseModel errorResponseModel);
        ClinicalQuestionBodyViewModel GetClinicalQuestionBodyPartDataById(int quetionId, int QBType, ref ErrorResponseModel errorResponseModel);
    }
}
