using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscriptionController : BaseAPIController
    {
        ISubscriptionService _subscriptionService;
        private readonly NIGACentrumContext _context;
        private readonly IRazorpayPaymentVerifier _razorpay;

        /// <summary>
        /// Used to initialize controller and inject subscription service
        /// </summary>
        /// <param name="subscriptionService"></param>
        public SubscriptionController(ISubscriptionService subscriptionService, NIGACentrumContext context, IRazorpayPaymentVerifier razorpay)
        {
            _subscriptionService = subscriptionService;
            _context = context;
            _razorpay = razorpay;
        }

        /// <summary>
        /// To get all lab entries
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubscription")]
        [Authorize(Policy = AdminAuthorizationPolicies.AccountOrAdmin)]
        [ProducesResponseType(typeof(List<SubscriptionModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubscription()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subscriptionEntryModel = _subscriptionService.GetSubscription(ref errorResponseModel);

                if (subscriptionEntryModel != null)
                {
                    return Ok(subscriptionEntryModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all lab entries
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubscriptionById/{packageDetailId}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AccountOrAdmin)]
        [ProducesResponseType(typeof(SubscriptionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabEntry(int packageDetailId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subscriptionModel = _subscriptionService.GetSubscriptionById(packageDetailId, ref errorResponseModel);

                if (subscriptionModel != null)
                {
                    return Ok(subscriptionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// Doctor activates a paid package after Razorpay checkout. Non-admin callers: the doctor comes from the JWT,
        /// the Razorpay signature must verify, the order must be paid in full for the package, and a payment id is used once.
        /// </summary>
        [HttpPost("SaveUpdateSubscription")]
        [ProducesResponseType(typeof(PatientLabOrderModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> SaveUpdateSubscription(SubscriptionModel subscriptionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                if (subscriptionModel == null)
                    return BadRequest(new { success = false, message = "Subscription details are required." });

                int userId = User.GetUserId();
                if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                {
                    var rejection = await VerifyPaidSubscriptionAsync(subscriptionModel, userId);
                    if (rejection != null)
                        return rejection;
                }

                var response = _subscriptionService.SaveSubscription(subscriptionModel, userId, ref errorResponseModel);

                if (response != null)
                {
                    return Ok(response);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        private async Task<IActionResult?> VerifyPaidSubscriptionAsync(SubscriptionModel model, int userId)
        {
            if ((model.PackageDetailId ?? 0) != 0)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "FORBIDDEN", message = "Only an administrator can change an existing subscription." });

            var doctorId = DoctorOwnership.GetDoctorId(User)
                ?? await _context.Doctors.AsNoTracking()
                    .Where(d => d.UserId == userId && !d.DeleteStatus)
                    .Select(d => (int?)d.DoctorId)
                    .FirstOrDefaultAsync();
            if (!doctorId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "FORBIDDEN", message = "Only a doctor can buy a subscription." });
            model.DoctorId = doctorId.Value;

            if (!_razorpay.Configured)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, code = "PAYMENT_NOT_CONFIGURED", message = "Payment verification is not configured on the server." });
            if (!_razorpay.SignatureValid(model.OrderId, model.PaymentId, model.TransactionId))
                return BadRequest(new { success = false, code = "PAYMENT_NOT_VERIFIED", message = "The payment could not be verified." });

            var paymentId = model.PaymentId!.Trim();
            if (await _context.PackageEntryDetails.AsNoTracking().AnyAsync(p => p.PaymentId == paymentId))
                return Conflict(new { success = false, code = "PAYMENT_ALREADY_USED", message = "This payment has already been used for a subscription." });

            var price = await _context.PackageMasters.AsNoTracking()
                .Where(p => p.PackageId == model.PackageId && !p.DeleteStatus)
                .Select(p => (decimal?)p.Amount)
                .FirstOrDefaultAsync();
            if (!price.HasValue)
                return BadRequest(new { success = false, code = "PACKAGE_NOT_FOUND", message = "Package not found." });

            var paidPaise = await _razorpay.AmountPaidPaiseAsync(model.OrderId!);
            if (!paidPaise.HasValue)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, code = "PAYMENT_LOOKUP_FAILED", message = "Could not confirm the payment with Razorpay. Try again shortly." });
            if (paidPaise.Value < (long)Math.Round(price.Value * 100m))
                return BadRequest(new { success = false, code = "PAYMENT_AMOUNT_MISMATCH", message = "The amount paid does not match the package price." });

            model.ActivationDate = DateTime.UtcNow;
            model.IsActive = true;
            return null;
        }
    }
}
