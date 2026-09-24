using System;
using System.Collections.Generic;
using System.Linq;

namespace Niga_Domain.Helpers
{
    public static class AppointmentSlotHelper
    {
        public static readonly int[] PresetIntervals = { 5, 10, 15, 20, 30, 60 };

        public const int MinIntervalMinutes = 1;

        public const int MaxIntervalMinutes = 180;

        public static bool IsAllowedInterval(int intervalMinutes) =>
            intervalMinutes >= MinIntervalMinutes && intervalMinutes <= MaxIntervalMinutes;

        /// <summary>
        /// APT-07.02 — server-side hours/break checks. Returns null when the window is valid.
        /// </summary>
        public static string? ValidateWorkingHoursAndBreak(
            TimeOnly workStart,
            TimeOnly workEnd,
            int intervalMinutes,
            TimeOnly? breakStart,
            TimeOnly? breakEnd)
        {
            if (workEnd <= workStart)
                return "Work end time must be after work start time.";

            if (breakStart.HasValue || breakEnd.HasValue)
            {
                if (!breakStart.HasValue || !breakEnd.HasValue
                    || breakEnd.Value <= breakStart.Value
                    || breakStart.Value < workStart
                    || breakEnd.Value > workEnd)
                {
                    return "Break must sit inside working hours and end after it starts.";
                }
            }

            var slots = GenerateSlots(workStart, workEnd, intervalMinutes);
            if (slots.Count == 0)
                return "Working hours do not produce any slots for this interval.";

            if (breakStart.HasValue && breakEnd.HasValue)
            {
                var bookable = slots.Count(slot =>
                    slot < breakStart.Value || slot >= breakEnd.Value);
                if (bookable == 0)
                    return "Break overlapping working hours must leave at least one bookable slot.";
            }

            return null;
        }

        public static bool IsTimeAlignedToInterval(TimeOnly appointmentTime, TimeOnly workStart, int intervalMinutes)
        {
            if (intervalMinutes <= 0)
            {
                return false;
            }

            var offsetMinutes = (appointmentTime.ToTimeSpan() - workStart.ToTimeSpan()).TotalMinutes;
            if (offsetMinutes < 0)
            {
                return false;
            }

            return appointmentTime.Second == 0 && offsetMinutes % intervalMinutes == 0;
        }

        public static List<TimeOnly> GenerateSlots(TimeOnly workStart, TimeOnly workEnd, int intervalMinutes)
        {
            var slots = new List<TimeOnly>();
            if (intervalMinutes <= 0 || workEnd < workStart)
            {
                return slots;
            }

            var current = workStart;
            while (current <= workEnd)
            {
                slots.Add(current);
                current = current.Add(TimeSpan.FromMinutes(intervalMinutes));
            }

            return slots;
        }

        public static bool IsPastSlot(DateTime appointmentDate, TimeOnly slotTime, DateTime now)
        {
            if (appointmentDate.Date > now.Date)
            {
                return false;
            }

            if (appointmentDate.Date < now.Date)
            {
                return true;
            }

            var slotDateTime = appointmentDate.Date.Add(slotTime.ToTimeSpan());
            return slotDateTime <= now;
        }
    }
}
