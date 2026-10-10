#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Errors;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interface;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Compatibility;

namespace Homeocentrum.Niga.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    [OldApiContract]
    public class CaseDetailsController : BaseAPIController
    {
        ICaseDetailsService _casedetailsService;

        public CaseDetailsController(ICaseDetailsService casedetailsService)
        {
            _casedetailsService = casedetailsService;
        }

        //[AllowAnonymous]

        /// <summary>
        /// To add new CaseDetail 
        /// </summary>
        /// <param name="casedetailsModel"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(CaseDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SaveCaseDetails(List<CaseDetailsModel> casedetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModel = _casedetailsService.SaveCaseDetails(casedetailsModel, ref errorResponseModel);

                if (remedyModel != null)
                {
                    return Ok(remedyModel);
                }
                if (errorResponseModel?.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound(new { success = false, message = errorResponseModel.Message ?? "Case detail not found." });
                return BadRequest(errorResponseModel?.Message ?? "Case details are required.");
            }
            catch (NullReferenceException)
            {
                return BadRequest("Invalid case-details payload.");
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get GetPatientBackHostory by patientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        [HttpGet("GetPatientBackHostoryById/{patientId}")]
        [ProducesResponseType(typeof(PatientAppointmentModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientBackHostoryById(long patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientAppointmentModel = _casedetailsService.GetPatientBackHostoryById(patientId, ref errorResponseModel);

                if (patientAppointmentModel != null)
                {
                    if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                    {
                        var jwtDoctorId = DoctorOwnership.GetDoctorId(User);
                        var owned = patientAppointmentModel
                            .Where(x => jwtDoctorId.HasValue && x.DoctorId == jwtDoctorId.Value)
                            .Select(ToOldBackHistoryShape)
                            .ToList();
                        return Ok(owned);
                    }
                    return Ok(patientAppointmentModel.Select(ToOldBackHistoryShape).ToList());
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        // Old API contract: AppointmentDate was a "yyyy-MM-dd" string and the WhatsApp opt-in fields did not exist.
        private static object ToOldBackHistoryShape(PatientAppointmentModel1 x) => new
        {
            x.PatientAppId,
            x.PatientId,
            x.PatientName,
            x.MobileNo,
            AppointmentDate = x.AppointmentDate?.ToString("yyyy-MM-dd"),
            x.AppointmentTime,
            x.Status,
            x.DeleteStatus,
            x.UserId,
            x.DoctorId,
            x.DoctorName,
            x.CaseId,
            x.HistoryNoteId,
            x.PaymentStatus,
            x.IsTele,
            x.VisitType,
            x.ConsultMode
        };
    }
}
