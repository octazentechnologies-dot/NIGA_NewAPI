using System.Collections.Generic;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces
{
    public interface IUserService
    {
        UserModel GetUserById(long userId, ref ErrorResponseModel errorResponseModel);
        string AddUser(UserModel model, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel);
        string RegisterDoctor(DoctorRegistrationModel model, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel);
        bool ActivateUser(UserModel model, ref ErrorResponseModel errorResponseModel);
        int GetCount(ref ErrorResponseModel errorResponseModel);
        List<NewUserModel> GetAllUser(ref ErrorResponseModel errorResponseModel);
        string DeleteUser(UserModel userModel, ref ErrorResponseModel errorResponseModel);
        string ForgetPassword(string email, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel);
    }
}
