using System;

namespace Niga_Domain.Master;

public partial class DoctorDailySchedule
{
    public int DoctorDailyScheduleId { get; set; }

    public int DoctorId { get; set; }

    public DateTime ScheduleDate { get; set; }

    public int SlotIntervalMinutes { get; set; }

    public TimeOnly WorkStartTime { get; set; }

    public TimeOnly WorkEndTime { get; set; }

    public TimeOnly? BreakStartTime { get; set; }

    public TimeOnly? BreakEndTime { get; set; }

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;
}
