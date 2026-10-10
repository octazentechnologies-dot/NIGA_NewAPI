#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    public interface ICaseDetailsService
    {
        ///// <summary>
        ///// Method is used for to get CaseDetail by CaseDetailId
        ///// </summary>
        ///// <param name="CaseDetailId"></param>
        ///// <param name="errorResponseModel"></param>
        ///// <returns></returns>
        //List<CaseDetailsModel> GetCaseDetailsById(long CaseDetailId, ref ErrorResponseModel errorResponseModel);

        ///// <summary>
        ///// Get details to edit rubric remedies
        ///// </summary>
        ///// <param name="subSectionId"></param>
        ///// <param name="errorResponseModel"></param>
        ///// <returns></returns>
        //CaseDetailsModel GetCaseDetailsToEdit(int subSectionId, int caseId, ref ErrorResponseModel errorResponseModel);


        /// <summary>remedyId
        /// Interface is used to save CaseDetail
        /// </summary>
        /// <param name="casedetailsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveCaseDetails(List<CaseDetailsModel> casedetailsModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Method is used for to get PatientBackHostory by PatientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<PatientAppointmentModel1> GetPatientBackHostoryById(long patientId, ref ErrorResponseModel errorResponseModel);
    }
}
