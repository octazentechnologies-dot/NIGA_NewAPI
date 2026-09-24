using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Homeocentrum.Niga.NewAPI.Domain.Enums
{
  public enum PatientsStatus : int
{
    [Display(Name = "WAITING")]
    Waiting = 1,

    [Display(Name = "WALK-IN")]
    Walk_In = 2,

    [Display(Name = "NOT ARRIVED")]
    NotArrived = 3,

    [Display(Name = "E-CONSULT")]
    E_Consult = 4,

    [Display(Name = "REMAINING")]
    Remaining = 5,

    [Display(Name = "COMPLETED")]
    Completed = 6
}
}

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        var member = enumValue.GetType().GetMember(enumValue.ToString())[0];
        var attr = member.GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? enumValue.ToString();
    }
}
