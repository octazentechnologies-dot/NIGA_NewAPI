using System;
using System.Collections.Generic;

namespace Niga_Domain.DTOs
{
    public class DoctorDailyScheduleModel
    {
        public int DoctorDailyScheduleId { get; set; }

        public int DoctorId { get; set; }

        public DateTime ScheduleDate { get; set; }

        public int SlotIntervalMinutes { get; set; }

        public TimeOnly WorkStartTime { get; set; }

        public TimeOnly WorkEndTime { get; set; }

        public bool IsLocked { get; set; } = true;
    }

    public class SaveDoctorDailyScheduleRequest
    {
        public int DoctorId { get; set; }

        public DateTime ScheduleDate { get; set; }

        public int SlotIntervalMinutes { get; set; }

        public TimeOnly WorkStartTime { get; set; }

        public TimeOnly WorkEndTime { get; set; }

        public long CreatedByUserId { get; set; }
    }

    public class GetDoctorDailyScheduleRequest
    {
        public int DoctorId { get; set; }

        public DateTime ScheduleDate { get; set; }
    }

    public class GetAppointmentSlotsRequest
    {
        public int DoctorId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public long? CurrentPatientAppId { get; set; }
    }

    public class AppointmentSlotModel
    {
        public string Time { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Status { get; set; } = "available";

        public long? PatientAppId { get; set; }

        public string? PatientName { get; set; }
    }

    public class AppointmentSlotsResponse
    {
        public int DoctorId { get; set; }

        public DateTime AppointmentDate { get; set; }

        public int IntervalMinutes { get; set; }

        public TimeOnly WorkStartTime { get; set; }

        public TimeOnly WorkEndTime { get; set; }

        public bool HasSchedule { get; set; }

        public List<AppointmentSlotModel> Slots { get; set; } = new List<AppointmentSlotModel>();
    }
}
