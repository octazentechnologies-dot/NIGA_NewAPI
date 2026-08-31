using System.Globalization;
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
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == PatientAppointmentModel.AppointmentDate.Value.Date &&
                    x.AppointmentTime.HasValue &&
                    PatientAppointmentModel.AppointmentTime.HasValue &&
                    x.AppointmentTime.Value == PatientAppointmentModel.AppointmentTime.Value);

                if (slotTaken)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                    errorResponseModel.Message = "This time slot is already booked. Please select another time.";
                    return string.Empty;
                }

                var patientAppEntity = new PatientAppointment
                {
                    PatientId = PatientAppointmentModel.PatientId,
                    UserId = PatientAppointmentModel.UserId,
                    DoctorId = PatientAppointmentModel.DoctorId,
                    AppointmentDate = PatientAppointmentModel.AppointmentDate,
                    AppointmentTime = PatientAppointmentModel.AppointmentTime,
                    Status = PatientAppointmentModel.Status,
                    DeleteStatus = PatientAppointmentModel.DeleteStatus
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
                x.DeleteStatus == false &&
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
                Message = "Appointment time updated successfully"
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
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == appointmentDate &&
                    x.AppointmentTime.HasValue)
                .ToListAsync();

            var bookedLookup = bookedAppointments.ToDictionary(
                x => x.AppointmentTime!.Value,
                x => x);

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

            if (AppointmentSlotHelper.IsPastSlot(appointmentDate, appointmentTime, DateTime.Now))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Past time slots cannot be booked.";
                return false;
            }

            var slotTaken = _context.PatientAppointments.Any(x =>
                x.DeleteStatus == false &&
                x.DoctorId == doctorId &&
                x.AppointmentDate.HasValue &&
                x.AppointmentDate.Value.Date == appointmentDate.Date &&
                x.AppointmentTime.HasValue &&
                x.AppointmentTime.Value == appointmentTime &&
                (!excludePatientAppId.HasValue || x.PatientAppId != excludePatientAppId.Value));

            if (slotTaken)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "This time slot is already booked. Please select another time.";
                return false;
            }

            return true;
        }
    }
}