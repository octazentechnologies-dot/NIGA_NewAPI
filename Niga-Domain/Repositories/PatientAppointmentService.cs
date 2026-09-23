using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using System.Net;

namespace Niga_Domain.Repositories
{
    /// <summary>
    /// Implementation for PatientAppointments operations
    /// </summary>
    public class PatientAppointmentService : IPatientAppointmentService
    {
        private readonly NIGACentrumContext _context;

        public PatientAppointmentService(NIGACentrumContext context)
        {
            _context = context;
        }

        public PatientAppointmentModel GetPatientAppById(long PatientAppId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var PatientAppEntity = _context.PatientAppointments
                .Include(x => x.Patient)
                .FirstOrDefault(x => x.PatientAppId == PatientAppId && x.DeleteStatus == false);

            if (PatientAppEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Patient Appointment not found";
                return null;
            }

            return new PatientAppointmentModel
            {
                PatientAppId = PatientAppEntity.PatientAppId,
                PatientId = PatientAppEntity.PatientId,
                DoctorId = PatientAppEntity.DoctorId,
                PatientName = PatientAppEntity.Patient.PatientName,
                MobileNo = PatientAppEntity.Patient.MobileNo,
                AppointmentDate = PatientAppEntity.AppointmentDate,
                AppointmentTime = PatientAppEntity.AppointmentTime,
                Status = PatientAppEntity.Status,
                UserId = PatientAppEntity.UserId,
                DeleteStatus = PatientAppEntity.DeleteStatus,
                IsWhatsAppOptIn = PatientAppEntity.Patient.IsWhatsAppOptIn,
                WhatsAppOptInDate = PatientAppEntity.Patient.WhatsAppOptInDate,
                VisitType = PatientAppEntity.VisitType,
                ConsultMode = PatientAppEntity.ConsultMode,
                PaymentStatus = PatientAppEntity.PaymentStatus,
                IsTele = PatientAppEntity.IsTele,
                PaymentMethod = PatientAppEntity.PaymentMethod,
                PayAtClinicAllowed = PatientAppEntity.PayAtClinicAllowed,
                CancelReasonCode = PatientAppEntity.CancelReasonCode,
                BookingChannel = PatientAppEntity.BookingChannel,
            };
        }

        public string SavePatientApp(PatientAppointmentModel PatientAppointmentModel, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            if (PatientAppointmentModel.PatientAppId == 0)
            {
                if (!PatientAppointmentModel.AppointmentDate.HasValue || !PatientAppointmentModel.AppointmentTime.HasValue)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                    errorResponseModel.Message = "Appointment date and time are required.";
                    return string.Empty;
                }

                if (!TryValidateAppointmentSlot(
                        PatientAppointmentModel.DoctorId,
                        PatientAppointmentModel.AppointmentDate.Value.Date,
                        PatientAppointmentModel.AppointmentTime.Value,
                        null,
                        ref errorResponseModel))
                {
                    return string.Empty;
                }

                var slotTaken = _context.PatientAppointments.Any(x =>
                    x.DoctorId == PatientAppointmentModel.DoctorId &&
                    x.DeleteStatus == false &&
                    x.Status != S3AppointmentRules.Cancelled &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == PatientAppointmentModel.AppointmentDate.Value.Date &&
                    x.AppointmentTime.HasValue &&
                    PatientAppointmentModel.AppointmentTime.HasValue &&
                    x.AppointmentTime.Value == PatientAppointmentModel.AppointmentTime.Value);

                if (slotTaken)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.Conflict;
                    errorResponseModel.Message = "This time slot is already booked. Please select another time.";
                    return string.Empty;
                }

                var mode = S3AppointmentRules.NormalizeMode(
                    PatientAppointmentModel.ConsultMode ?? PatientAppointmentModel.VisitType);

                var patientAppEntity = new PatientAppointment
                {
                    PatientId = PatientAppointmentModel.PatientId,
                    UserId = PatientAppointmentModel.UserId,
                    DoctorId = PatientAppointmentModel.DoctorId,
                    AppointmentDate = PatientAppointmentModel.AppointmentDate,
                    AppointmentTime = PatientAppointmentModel.AppointmentTime,
                    Status = string.IsNullOrWhiteSpace(PatientAppointmentModel.Status)
                        ? "WAITING"
                        : PatientAppointmentModel.Status,
                    DeleteStatus = PatientAppointmentModel.DeleteStatus ?? false,
                    VisitType = mode,
                    ConsultMode = mode,
                    IsTele = mode == S3AppointmentRules.Tele,
                    PaymentStatus = S3AppointmentRules.Unpaid,
                    PayAtClinicAllowed = true,
                    BookingChannel = string.IsNullOrWhiteSpace(PatientAppointmentModel.BookingChannel)
                        ? "Staff"
                        : PatientAppointmentModel.BookingChannel.Trim()
                };
                _context.PatientAppointments.Add(patientAppEntity);
                _context.SaveChanges();
                return "Patient Appointment Saved Successfully";
            }

            var existingEntity = _context.PatientAppointments
                .FirstOrDefault(x => x.PatientAppId == PatientAppointmentModel.PatientAppId);

            if (existingEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Appointment not found.";
                return string.Empty;
            }

            if (S3AppointmentRules.IsCancelled(PatientAppointmentModel.Status))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Use CancelAppointment. A cancel reason is required.";
                return string.Empty;
            }

            existingEntity.PatientId = PatientAppointmentModel.PatientId;
            existingEntity.UserId = PatientAppointmentModel.UserId;
            existingEntity.DoctorId = PatientAppointmentModel.DoctorId;
            existingEntity.Status = PatientAppointmentModel.Status;
            existingEntity.DeleteStatus = PatientAppointmentModel.DeleteStatus;
            existingEntity.AppointmentDate = PatientAppointmentModel.AppointmentDate ?? existingEntity.AppointmentDate;
            existingEntity.AppointmentTime = PatientAppointmentModel.AppointmentTime ?? existingEntity.AppointmentTime;

            _context.SaveChanges();
            return "Patient Appointment Updated Successfully";
        }

        public List<PatientModel> GetCasesByUser(long userId, ref ErrorResponseModel errorResponseModel)
        {
            var PatientModelList = new List<PatientModel>();
            errorResponseModel = new ErrorResponseModel();
            var doctorEntity = _context.Doctors.FirstOrDefault(x => x.UserId == userId);

            if (doctorEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Doctor not found";
                return PatientModelList;
            }

            var caseEntityList = _context.CaseEntryDetails
                .Include(x => x.Patient)
                .Where(x => x.DoctorId == doctorEntity.DoctorId && x.DeleteStatus == false)
                .ToList();

            if (caseEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Patient not found";
            }

            var patientIds = caseEntityList.Select(x => x.PatientId).Distinct().ToList();
            var lastVisits = _context.PatientAppointments
                .Where(a => patientIds.Contains(a.PatientId) && a.DeleteStatus != true)
                .GroupBy(a => a.PatientId)
                .Select(g => new { PatientId = g.Key, Last = g.Max(x => x.AppointmentDate) })
                .ToList()
                .ToDictionary(x => x.PatientId, x => x.Last);

            caseEntityList.ForEach(item =>
            {
                PatientModelList.Add(new PatientModel
                {
                    CaseId = item.CaseId,
                    PatientID = item.PatientId,
                    PatientName = item.Patient.PatientName,
                    MobileNo = item.Patient.MobileNo,
                    Gender = item.Patient.Gender,
                    Address = item.Patient.Address,
                    DateOfBirth = item.Patient.DateOfBirth,
                    IsWhatsAppOptIn = item.Patient.IsWhatsAppOptIn,
                    WhatsAppOptInDate = item.Patient.WhatsAppOptInDate,
                    LastVisitAt = lastVisits.TryGetValue(item.PatientId, out var last) ? last : null,
                });
            });

            return PatientModelList;
        }



        public PatientAppointmentModel UpdateAppointmentStatus(UpdateAppointmentStatusModel model, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var appointment = _context.PatientAppointments
                .Include(x => x.Patient)
                .FirstOrDefault(x =>
                    x.PatientAppId == model.PatientAppId &&
                    x.DeleteStatus == false);

            if (appointment == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Appointment not found";
                return null;
            }

            // Status validation
            var allowedStatus = new List<string>
    {
        "WAITING",
        "WALK-IN",
        "NOT ARRIVED",
        "E-CONSULT",
        "REMAINING",
        "COMPLETED"
    };

            if (S3AppointmentRules.IsCancelled(model.Status))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Use CancelAppointment. A cancel reason is required.";
                return null;
            }

            if (!allowedStatus.Contains(model.Status.ToUpper()))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Invalid appointment status";
                return null;
            }

            // Update status
            appointment.Status = model.Status.ToUpper();
            _context.SaveChanges();

            return new PatientAppointmentModel
            {
                PatientAppId = appointment.PatientAppId,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                UserId = appointment.UserId,
                DeleteStatus = appointment.DeleteStatus,
                IsWhatsAppOptIn = appointment.Patient.IsWhatsAppOptIn,
                WhatsAppOptInDate = appointment.Patient.WhatsAppOptInDate,
                Message = "Appointment status updated successfully"
            };
        }

        public PatientAppointmentModel UpdateAppointmentTime(
            UpdateAppointmentTimeModel model,
            ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            if (!model.AppointmentTime.HasValue)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "AppointmentTime is required";
                return null;
            }

            var appointment = _context.PatientAppointments
                .Include(x => x.Patient)
                .FirstOrDefault(x =>
                    x.PatientAppId == model.PatientAppId &&
                    x.DeleteStatus == false);

            if (appointment == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Appointment not found";
                return null;
            }

            var appointmentDate = model.AppointmentDate?.Date ?? appointment.AppointmentDate?.Date;
            if (!appointmentDate.HasValue)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "AppointmentDate is required";
                return null;
            }

            if (!TryValidateAppointmentSlot(
                    appointment.DoctorId,
                    appointmentDate.Value,
                    model.AppointmentTime.Value,
                    model.PatientAppId,
                    ref errorResponseModel))
            {
                return null;
            }

            var slotTaken = _context.PatientAppointments.Any(x =>
                x.PatientAppId != model.PatientAppId &&
                x.DoctorId == appointment.DoctorId &&
                x.DeleteStatus != true &&
                x.Status != S3AppointmentRules.Cancelled &&
                x.AppointmentDate.HasValue &&
                x.AppointmentDate.Value.Date == appointmentDate.Value &&
                x.AppointmentTime.HasValue &&
                x.AppointmentTime.Value == model.AppointmentTime.Value);

            if (slotTaken)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "This time slot is already booked. Please select another time.";
                return null;
            }

            appointment.AppointmentDate = appointmentDate.Value;
            appointment.AppointmentTime = model.AppointmentTime;
            _context.SaveChanges();

            return new PatientAppointmentModel
            {
                PatientAppId = appointment.PatientAppId,
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient.PatientName,
                MobileNo = appointment.Patient.MobileNo,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                UserId = appointment.UserId,
                DeleteStatus = appointment.DeleteStatus,
                IsWhatsAppOptIn = appointment.Patient.IsWhatsAppOptIn,
                WhatsAppOptInDate = appointment.Patient.WhatsAppOptInDate,
                Message = "Appointment time updated. This does not notify the patient. Use Reschedule for the patient-notified path."
            };
        }

        public async Task<List<PatientAppointmentModel>> GetAppointmentsByDateAsync(
            GetAppointmentsByDateRequest request)
        {
            var appointmentDate = request.AppointmentDate.Date;

            var appointments = await _context.PatientAppointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x =>
                    x.UserId == request.UserId &&
                    x.DeleteStatus == false &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == appointmentDate)
                .OrderBy(x => x.AppointmentTime)
                .ThenBy(x => x.PatientAppId)
                .ToListAsync();

            return appointments.Select(appointment => new PatientAppointmentModel
            {
                PatientAppId = appointment.PatientAppId,
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient.PatientName,
                MobileNo = appointment.Patient.MobileNo,
                Email = appointment.Patient.Email,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                UserId = appointment.UserId,
                DeleteStatus = appointment.DeleteStatus,
                VisitType = appointment.VisitType,
                ConsultMode = appointment.ConsultMode,
                PaymentStatus = appointment.PaymentStatus,
                IsTele = appointment.IsTele,
                PaymentMethod = appointment.PaymentMethod,
                PayAtClinicAllowed = appointment.PayAtClinicAllowed,
                CancelReasonCode = appointment.CancelReasonCode,
                BookingChannel = appointment.BookingChannel,
                Address = appointment.Patient.Address,
                Age = appointment.Patient.Age,
                Gender = appointment.Patient.Gender,
                DateOfBirth = appointment.Patient.DateOfBirth,
                IsWhatsAppOptIn = appointment.Patient.IsWhatsAppOptIn,
                WhatsAppOptInDate = appointment.Patient.WhatsAppOptInDate,
            }).ToList();
        }

        public async Task<bool> PatientExistsAsync(int patientId)
        {
            return await _context.Patients
                .AsNoTracking()
                .AnyAsync(x => x.PatientId == patientId && x.DeleteStatus == false);
        }

        public async Task<PaginatedResult<PatientAppointmentListItemModel>> GetAppointmentListByPatientIdAsync(
            GetAppointmentListByPatientIdRequest request)
        {
            var paginationRequest = new PaginationRequestModel
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            var query = _context.PatientAppointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x => x.PatientId == request.PatientId && x.DeleteStatus == false)
                .ApplyPatientAppointmentSorting(paginationRequest);

            var paginatedEntities = await query.ToPaginatedResultAsync(paginationRequest);

            return new PaginatedResult<PatientAppointmentListItemModel>
            {
                Items = paginatedEntities.Items.Select(MapToListItem).ToList(),
                PageNumber = paginatedEntities.PageNumber,
                PageSize = paginatedEntities.PageSize,
                TotalRecords = paginatedEntities.TotalRecords,
                TotalPages = paginatedEntities.TotalPages
            };
        }

        private static PatientAppointmentListItemModel MapToListItem(PatientAppointment entity)
        {
            string? appointmentDate = null;
            string? appointmentTime = null;
            string? appointmentDateTime = null;

            if (entity.AppointmentDate.HasValue)
            {
                appointmentDate = entity.AppointmentDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (entity.AppointmentTime.HasValue)
            {
                appointmentTime = entity.AppointmentTime.Value.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }

            if (entity.AppointmentDate.HasValue && entity.AppointmentTime.HasValue)
            {
                var combined = entity.AppointmentDate.Value.Date.Add(entity.AppointmentTime.Value.ToTimeSpan());
                appointmentDateTime = combined.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            }
            else if (entity.AppointmentDate.HasValue)
            {
                appointmentDateTime = entity.AppointmentDate.Value.ToString(
                    "yyyy-MM-ddTHH:mm:ss",
                    CultureInfo.InvariantCulture);
            }

            return new PatientAppointmentListItemModel
            {
                PatientAppId = entity.PatientAppId,
                PatientId = entity.PatientId,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                AppointmentDateTime = appointmentDateTime,
                Status = entity.Status,
                DoctorId = entity.DoctorId,
                UserId = entity.UserId,
                IsWhatsAppOptIn = entity.Patient?.IsWhatsAppOptIn ?? false,
                WhatsAppOptInDate = entity.Patient?.WhatsAppOptInDate,
                PaymentStatus = entity.PaymentStatus,
                IsTele = entity.IsTele,
                VisitType = entity.VisitType,
                ConsultMode = entity.ConsultMode,
                PaymentMethod = entity.PaymentMethod,
                PayAtClinicAllowed = entity.PayAtClinicAllowed,
            };
        }

        public async Task<DoctorDailyScheduleModel?> GetDailyScheduleAsync(GetDoctorDailyScheduleRequest request)
        {
            var scheduleDate = request.ScheduleDate.Date;
            var schedule = await _context.DoctorDailySchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.DoctorId == request.DoctorId &&
                    x.ScheduleDate == scheduleDate);

            if (schedule == null)
            {
                return null;
            }

            return MapDailySchedule(schedule);
        }

        public async Task<(DoctorDailyScheduleModel? Schedule, ErrorResponseModel? Error)> SaveDailyScheduleAsync(
            SaveDoctorDailyScheduleRequest request)
        {
            var errorResponseModel = new ErrorResponseModel();
            var scheduleDate = request.ScheduleDate.Date;
            var today = DateTime.Today;

            if (scheduleDate < today)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Schedule can only be created for today or future dates.";
                return (null, errorResponseModel);
            }

            if (!AppointmentSlotHelper.IsAllowedInterval(request.SlotIntervalMinutes))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = $"Invalid slot interval. Enter a whole number between {AppointmentSlotHelper.MinIntervalMinutes} and {AppointmentSlotHelper.MaxIntervalMinutes} minutes.";
                return (null, errorResponseModel);
            }

            if (request.WorkEndTime <= request.WorkStartTime)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Work end time must be after work start time.";
                return (null, errorResponseModel);
            }

            if (request.BreakStartTime.HasValue || request.BreakEndTime.HasValue)
            {
                if (!request.BreakStartTime.HasValue || !request.BreakEndTime.HasValue
                    || request.BreakEndTime <= request.BreakStartTime
                    || request.BreakStartTime < request.WorkStartTime
                    || request.BreakEndTime > request.WorkEndTime)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                    errorResponseModel.Message = "Break must sit inside working hours and end after it starts.";
                    return (null, errorResponseModel);
                }
            }

            var doctorExists = await _context.Doctors
                .AsNoTracking()
                .AnyAsync(x => x.DoctorId == request.DoctorId && x.DeleteStatus == false);

            if (!doctorExists)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Doctor not found.";
                return (null, errorResponseModel);
            }

            var existingSchedule = await _context.DoctorDailySchedules
                .FirstOrDefaultAsync(x =>
                    x.DoctorId == request.DoctorId &&
                    x.ScheduleDate == scheduleDate);

            if (existingSchedule != null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Daily schedule is already set for this date and cannot be changed.";
                return (null, errorResponseModel);
            }

            var entity = new DoctorDailySchedule
            {
                DoctorId = request.DoctorId,
                ScheduleDate = scheduleDate,
                SlotIntervalMinutes = request.SlotIntervalMinutes,
                WorkStartTime = request.WorkStartTime,
                WorkEndTime = request.WorkEndTime,
                BreakStartTime = request.BreakStartTime,
                BreakEndTime = request.BreakEndTime,
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt = DateTime.UtcNow,
            };

            _context.DoctorDailySchedules.Add(entity);
            await _context.SaveChangesAsync();

            return (MapDailySchedule(entity), null);
        }

        public async Task<AppointmentSlotsResponse> GetAppointmentSlotsAsync(GetAppointmentSlotsRequest request)
        {
            var appointmentDate = request.AppointmentDate.Date;
            var response = new AppointmentSlotsResponse
            {
                DoctorId = request.DoctorId,
                AppointmentDate = appointmentDate,
            };

            var schedule = await _context.DoctorDailySchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.DoctorId == request.DoctorId &&
                    x.ScheduleDate == appointmentDate);

            if (schedule == null)
            {
                response.HasSchedule = false;
                return response;
            }

            response.HasSchedule = true;
            response.IntervalMinutes = schedule.SlotIntervalMinutes;
            response.WorkStartTime = schedule.WorkStartTime;
            response.WorkEndTime = schedule.WorkEndTime;

            var bookedAppointments = await _context.PatientAppointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x =>
                    x.DoctorId == request.DoctorId &&
                    x.DeleteStatus == false &&
                    x.Status != S3AppointmentRules.Cancelled &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == appointmentDate &&
                    x.AppointmentTime.HasValue)
                .ToListAsync();

            var bookedLookup = bookedAppointments
                .GroupBy(x => x.AppointmentTime!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.PatientAppId).First());

            var generatedSlots = AppointmentSlotHelper.GenerateSlots(
                schedule.WorkStartTime,
                schedule.WorkEndTime,
                schedule.SlotIntervalMinutes);

            var now = DateTime.Now;
            response.Slots = generatedSlots.Select(slotTime =>
            {
                var status = "available";
                long? patientAppId = null;
                string? patientName = null;

                if (request.CurrentPatientAppId.HasValue &&
                    bookedLookup.TryGetValue(slotTime, out var currentAppointment) &&
                    currentAppointment.PatientAppId == request.CurrentPatientAppId.Value)
                {
                    status = "current";
                    patientAppId = currentAppointment.PatientAppId;
                    patientName = currentAppointment.Patient?.PatientName;
                }
                else if (bookedLookup.TryGetValue(slotTime, out var bookedAppointment))
                {
                    status = "booked";
                    patientAppId = bookedAppointment.PatientAppId;
                    patientName = bookedAppointment.Patient?.PatientName;
                }
                else if (schedule.BreakStartTime.HasValue
                    && schedule.BreakEndTime.HasValue
                    && slotTime >= schedule.BreakStartTime.Value
                    && slotTime < schedule.BreakEndTime.Value)
                {
                    status = "break";
                }
                else if (AppointmentSlotHelper.IsPastSlot(appointmentDate, slotTime, now))
                {
                    status = "past";
                }

                return new AppointmentSlotModel
                {
                    Time = slotTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                    Label = slotTime.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                    Status = status,
                    PatientAppId = patientAppId,
                    PatientName = patientName,
                };
            }).ToList();

            return response;
        }

        private static DoctorDailyScheduleModel MapDailySchedule(DoctorDailySchedule schedule) =>
            new DoctorDailyScheduleModel
            {
                DoctorDailyScheduleId = schedule.DoctorDailyScheduleId,
                DoctorId = schedule.DoctorId,
                ScheduleDate = schedule.ScheduleDate,
                SlotIntervalMinutes = schedule.SlotIntervalMinutes,
                WorkStartTime = schedule.WorkStartTime,
                WorkEndTime = schedule.WorkEndTime,
                BreakStartTime = schedule.BreakStartTime,
                BreakEndTime = schedule.BreakEndTime,
                IsLocked = true,
            };

        private bool TryValidateAppointmentSlot(
            int doctorId,
            DateTime appointmentDate,
            TimeOnly appointmentTime,
            long? excludePatientAppId,
            ref ErrorResponseModel errorResponseModel)
        {
            if (appointmentDate.Date < DateTime.Today)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Appointment date cannot be in the past.";
                return false;
            }

            var schedule = _context.DoctorDailySchedules
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.DoctorId == doctorId &&
                    x.ScheduleDate == appointmentDate.Date);

            if (schedule == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Daily appointment schedule is not configured for this date.";
                return false;
            }

            if (!AppointmentSlotHelper.IsTimeAlignedToInterval(
                    appointmentTime,
                    schedule.WorkStartTime,
                    schedule.SlotIntervalMinutes))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Selected time does not match the configured slot interval.";
                return false;
            }

            if (appointmentTime < schedule.WorkStartTime || appointmentTime > schedule.WorkEndTime)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Selected time is outside working hours.";
                return false;
            }

            var allowedSlots = AppointmentSlotHelper.GenerateSlots(
                schedule.WorkStartTime,
                schedule.WorkEndTime,
                schedule.SlotIntervalMinutes);

            if (!allowedSlots.Contains(appointmentTime))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Selected time is not a valid appointment slot.";
                return false;
            }

            if (schedule.BreakStartTime.HasValue
                && schedule.BreakEndTime.HasValue
                && appointmentTime >= schedule.BreakStartTime.Value
                && appointmentTime < schedule.BreakEndTime.Value)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Selected time falls in the break.";
                return false;
            }

            if (AppointmentSlotHelper.IsPastSlot(appointmentDate, appointmentTime, DateTime.Now))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Past time slots cannot be booked.";
                return false;
            }

            var slotTaken = _context.PatientAppointments.Any(x =>
                x.DeleteStatus == false &&
                x.Status != S3AppointmentRules.Cancelled &&
                x.DoctorId == doctorId &&
                x.AppointmentDate.HasValue &&
                x.AppointmentDate.Value.Date == appointmentDate.Date &&
                x.AppointmentTime.HasValue &&
                x.AppointmentTime.Value == appointmentTime &&
                (!excludePatientAppId.HasValue || x.PatientAppId != excludePatientAppId.Value));

            if (slotTaken)
            {
                errorResponseModel.StatusCode = HttpStatusCode.Conflict;
                errorResponseModel.Message = "This time slot is already booked. Please select another time.";
                return false;
            }

            return true;
        }

        private void CopyS3Fields(PatientAppointment entity, PatientAppointmentModel model)
        {
            model.VisitType = entity.VisitType;
            model.ConsultMode = entity.ConsultMode;
            model.PaymentStatus = entity.PaymentStatus;
            model.IsTele = entity.IsTele;
            model.PaymentMethod = entity.PaymentMethod;
            model.PayAtClinicAllowed = entity.PayAtClinicAllowed;
            model.CancelReasonCode = entity.CancelReasonCode;
            model.BookingChannel = entity.BookingChannel;
        }

        public async Task<AppointmentMutationResult> RescheduleAppointmentAsync(
            RescheduleAppointmentRequest request,
            long byUserId,
            string? byRole)
        {
            var result = new AppointmentMutationResult();
            var appointment = await _context.PatientAppointments
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.PatientAppId == request.PatientAppId && x.DeleteStatus != true);

            if (appointment == null)
            {
                result.StatusCode = 404;
                result.Message = "Appointment not found";
                return result;
            }

            if (S3AppointmentRules.IsCancelled(appointment.Status))
            {
                result.StatusCode = 409;
                result.Message = "A cancelled appointment cannot be rescheduled.";
                return result;
            }

            if (!request.AppointmentTime.HasValue || !request.AppointmentDate.HasValue)
            {
                result.StatusCode = 400;
                result.Message = "Appointment date and time are required.";
                return result;
            }

            var error = new ErrorResponseModel();
            if (!TryValidateAppointmentSlot(
                    appointment.DoctorId,
                    request.AppointmentDate.Value.Date,
                    request.AppointmentTime.Value,
                    request.PatientAppId,
                    ref error))
            {
                result.StatusCode = error.StatusCode == HttpStatusCode.Conflict ? 409 : (int)(error.StatusCode == 0 ? HttpStatusCode.BadRequest : error.StatusCode);
                if (result.StatusCode == 400 && error.Message != null && error.Message.Contains("already booked", StringComparison.OrdinalIgnoreCase))
                    result.StatusCode = 409;
                result.Message = error.Message;
                result.Alternatives = await NextOpenSlotsAsync(appointment.DoctorId, request.AppointmentDate.Value.Date, request.PatientAppId);
                return result;
            }

            var patientBusy = await _context.PatientAppointments.AnyAsync(x =>
                x.PatientId == appointment.PatientId &&
                x.PatientAppId != appointment.PatientAppId &&
                x.DeleteStatus != true &&
                x.Status != S3AppointmentRules.Cancelled &&
                x.AppointmentDate.HasValue &&
                x.AppointmentDate.Value.Date == request.AppointmentDate.Value.Date &&
                x.AppointmentTime.HasValue &&
                x.AppointmentTime.Value == request.AppointmentTime.Value);

            if (patientBusy)
            {
                result.StatusCode = 409;
                result.Message = "This patient is already booked at that time.";
                result.Alternatives = await NextOpenSlotsAsync(appointment.DoctorId, request.AppointmentDate.Value.Date, request.PatientAppId);
                return result;
            }

            var oldValue = $"{appointment.AppointmentDate:yyyy-MM-dd} {appointment.AppointmentTime}";
            var newValue = $"{request.AppointmentDate:yyyy-MM-dd} {request.AppointmentTime}";
            appointment.AppointmentDate = request.AppointmentDate.Value.Date;
            appointment.AppointmentTime = request.AppointmentTime;
            await _context.SaveChangesAsync();
            await WriteChangeLogAsync(appointment.PatientAppId, "Reschedule", oldValue, newValue, byUserId, byRole, request.Reason);

            result.StatusCode = 200;
            result.Message = "Appointment rescheduled.";
            result.Appointment = MapCore(appointment);
            result.Appointment.Message = result.Message;
            return result;
        }

        public async Task<AppointmentMutationResult> CancelAppointmentAsync(
            CancelAppointmentRequest request,
            long byUserId,
            string? byRole)
        {
            var result = new AppointmentMutationResult();
            if (request == null || string.IsNullOrWhiteSpace(request.ReasonCode)
                || !S3AppointmentRules.CancelReasons.Contains(request.ReasonCode, StringComparer.OrdinalIgnoreCase))
            {
                result.StatusCode = 400;
                result.Message = "ReasonCode must be PatientRequest, DoctorUnavailable, Duplicate, or Other.";
                return result;
            }

            if (request.ReasonCode.Equals("Other", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.ReasonText))
            {
                result.StatusCode = 400;
                result.Message = "ReasonText is required when ReasonCode is Other.";
                return result;
            }

            var appointment = await _context.PatientAppointments
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.PatientAppId == request.PatientAppId && x.DeleteStatus != true);

            if (appointment == null)
            {
                result.StatusCode = 404;
                result.Message = "Appointment not found";
                return result;
            }

            if (S3AppointmentRules.IsCancelled(appointment.Status))
            {
                result.StatusCode = 409;
                result.Message = "Appointment is already cancelled.";
                return result;
            }

            var oldStatus = appointment.Status;
            appointment.Status = S3AppointmentRules.Cancelled;
            appointment.CancelReasonCode = request.ReasonCode;
            appointment.CancelReasonText = request.ReasonText;
            appointment.CancelledBy = byUserId;
            appointment.CancelledAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await WriteChangeLogAsync(
                appointment.PatientAppId,
                "Cancel",
                oldStatus,
                S3AppointmentRules.Cancelled,
                byUserId,
                byRole,
                request.ReasonCode);

            result.StatusCode = 200;
            result.Message = "Appointment cancelled. The slot is free. No refund was sent.";
            result.Appointment = MapCore(appointment);
            return result;
        }

        public async Task<List<AppointmentChangeLogItem>> GetChangeLogAsync(int patientAppId)
        {
            return await _context.Database.SqlQuery<AppointmentChangeLogItem>(
                $@"SELECT AppointmentChangeLogId, PatientAppId, Action, OldValue, NewValue, ByUserId, ByRole, Reason, At
                   FROM dbo.AppointmentChangeLog
                   WHERE PatientAppId = {patientAppId}
                   ORDER BY At DESC").ToListAsync();
        }

        public async Task<AppointmentMutationResult> PatchVisitTypeAsync(int patientAppId, string? visitType, string? consultMode)
        {
            var result = new AppointmentMutationResult();
            var appointment = await _context.PatientAppointments
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.PatientAppId == patientAppId && x.DeleteStatus != true);
            if (appointment == null)
            {
                result.StatusCode = 404;
                result.Message = "Appointment not found";
                return result;
            }

            var mode = S3AppointmentRules.NormalizeMode(consultMode ?? visitType, appointment.ConsultMode ?? S3AppointmentRules.InClinic);
            appointment.VisitType = mode;
            appointment.ConsultMode = mode;
            appointment.IsTele = mode == S3AppointmentRules.Tele;
            await _context.SaveChangesAsync();
            result.StatusCode = 200;
            result.Appointment = MapCore(appointment);
            result.Message = "Visit type updated for this appointment only.";
            return result;
        }

        public async Task<AppointmentMutationResult> CallNextAsync(int doctorId)
        {
            var result = new AppointmentMutationResult();
            var today = DateTime.Today;
            var next = await _context.PatientAppointments
                .Include(x => x.Patient)
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.DeleteStatus != true &&
                    x.Status == "WAITING" &&
                    x.CalledAt == null &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == today)
                .OrderBy(x => x.AppointmentTime)
                .ThenBy(x => x.PatientAppId)
                .FirstOrDefaultAsync();

            if (next == null)
            {
                result.StatusCode = 404;
                result.Message = "No waiting patient to call.";
                return result;
            }

            // REC-08.02 — calling the next waiting patient stamps the time and moves them onto the board status.
            next.CalledAt = DateTime.Now;
            if (string.Equals(next.Status, "WAITING", StringComparison.OrdinalIgnoreCase))
                next.Status = next.IsTele == true ? "E-CONSULT" : "WALK-IN";
            await _context.SaveChangesAsync();
            result.StatusCode = 200;
            result.Message = "Next patient called.";
            result.Appointment = MapCore(next);
            return result;
        }

        public async Task<List<PatientAppointmentModel>> GetQueueAsync(int doctorId)
        {
            var today = DateTime.Today;
            var rows = await _context.PatientAppointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.DeleteStatus != true &&
                    x.Status != S3AppointmentRules.Cancelled &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == today &&
                    (x.Status == "WAITING" || x.Status == "WALK-IN" || x.Status == "E-CONSULT"))
                .OrderBy(x => x.AppointmentTime)
                .ThenBy(x => x.PatientAppId)
                .ToListAsync();

            return rows.Select(MapCore).ToList();
        }

        private async Task<List<AppointmentSlotModel>> NextOpenSlotsAsync(int doctorId, DateTime date, long currentId)
        {
            var slots = await GetAppointmentSlotsAsync(new GetAppointmentSlotsRequest
            {
                DoctorId = doctorId,
                AppointmentDate = date,
                CurrentPatientAppId = currentId
            });
            return slots.Slots.Where(s => s.Status == "available").Take(3).ToList();
        }

        private async Task WriteChangeLogAsync(int patientAppId, string action, string? oldValue, string? newValue, long byUserId, string? byRole, string? reason)
        {
            var at = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.AppointmentChangeLog (PatientAppId, Action, OldValue, NewValue, ByUserId, ByRole, Reason, At)
                VALUES ({patientAppId}, {action}, {oldValue}, {newValue}, {byUserId}, {byRole}, {reason}, {at})");
        }

        private PatientAppointmentModel MapCore(PatientAppointment appointment)
        {
            var model = new PatientAppointmentModel
            {
                PatientAppId = appointment.PatientAppId,
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient?.PatientName,
                MobileNo = appointment.Patient?.MobileNo,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                UserId = appointment.UserId,
                DeleteStatus = appointment.DeleteStatus,
            };
            CopyS3Fields(appointment, model);
            return model;
        }
    }
}