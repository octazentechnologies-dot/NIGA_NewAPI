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
