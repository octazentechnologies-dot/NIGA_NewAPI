using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    public interface IClipboardRubricsService
    {


        /// <summary>
        /// Method is used for get all the Clipboard Rubrics
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<ClipboardRubricsModel> GetClipboardRubrics(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to save Clipboard Rubrics
        /// </summary>
        /// <param name="clipboardRubricsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveClipboardRubrics(List<ClipboardRubricsModel> clipboardRubricsModel, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the Clipboard Rubrics by patientid
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<ClipboardRubricsModel> GetClipboardRubricsPatientId(int PatientId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to delete Clipboard Rubrics.
        /// </summary>
        /// <param name="clinicalquestionsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteClipboardRubrics(ClipboardRubricsModel clipboardRubricsModel, ref ErrorResponseModel errorResponseModel);

        // List<ClipboardRubricsModel1> GetRubricsDetailsBySubsectionId(string SubsectionId, ref ErrorResponseModel errorResponseModel);
        List<ClipboardRubricsModel1> GetRubricsDetailsBySubsectionId(List<ClipboardRubricsModel1> lstIntensity, ref ErrorResponseModel errorResponseModel);

        ClipboardRemedyModel GetCommanUnCommanRubricsDetailsBySubsectionId(List<ClipboardRubricsModel1> lstIntensity, ref ErrorResponseModel errorResponseModel);

        List<RepertorizarionRemedyModel> GetRepertorizarionRemedy(RepertorizarionRemedyInputModel inputModel, ref ErrorResponseModel errorResponseModel);

        ClipboardRemedyModel GetCommanUnCommanRubricsDetailsByElemation(ClipboardRUbricModel clipboardRUbricModel, ref ErrorResponseModel errorResponseModel);

        ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionId1(List<ClipboardRubricsModel1> lstIntensity, ref ErrorResponseModel errorResponseModel);

        ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionId1(ClipboardRUbricModel clipboardRUbricModel, ref ErrorResponseModel errorResponseModel);
        //orignal repertory
        ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionIdFinal(List<ClipboardRubricsModel1> lstIntensity, ref ErrorResponseModel errorResponseModel);

       
        ClipboardRemedyNewModel GetCommanUnCommanEliminationData(ClipboardRUbricModel clipboardRUbricModel, ref ErrorResponseModel errorResponseModel);
    }
}
