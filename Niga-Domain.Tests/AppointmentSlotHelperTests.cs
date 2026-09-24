using Niga_Domain.Helpers;
using Xunit;

namespace Niga_Domain.Tests;

public class AppointmentSlotHelperTests
{
    [Fact]
    public void ValidateWorkingHoursAndBreak_AcceptsBreakInsideHours()
    {
        var error = AppointmentSlotHelper.ValidateWorkingHoursAndBreak(
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            15,
            new TimeOnly(10, 30),
            new TimeOnly(10, 45));

        Assert.Null(error);
    }

    [Fact]
    public void ValidateWorkingHoursAndBreak_RejectsInvertedHours()
    {
        var error = AppointmentSlotHelper.ValidateWorkingHoursAndBreak(
            new TimeOnly(12, 0),
            new TimeOnly(9, 0),
            15,
            null,
            null);

        Assert.Equal("Work end time must be after work start time.", error);
    }

    [Fact]
    public void ValidateWorkingHoursAndBreak_RejectsBreakOutsideHours()
    {
        var error = AppointmentSlotHelper.ValidateWorkingHoursAndBreak(
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            15,
            new TimeOnly(8, 0),
            new TimeOnly(8, 30));

        Assert.Equal("Break must sit inside working hours and end after it starts.", error);
    }

    [Fact]
    public void ValidateWorkingHoursAndBreak_RejectsBreakCoveringEverySlot()
    {
        var error = AppointmentSlotHelper.ValidateWorkingHoursAndBreak(
            new TimeOnly(9, 0),
            new TimeOnly(9, 10),
            15,
            new TimeOnly(9, 0),
            new TimeOnly(9, 10));

        Assert.Equal("Break overlapping working hours must leave at least one bookable slot.", error);
    }
}
