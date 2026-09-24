using System;
using System.Net;
using System.Net.Mail;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    public class EmailSenderService
    {
        public string LastError { get; private set; }

        public bool SendMail(EmailSenderModel emailSenderModel, SmtpSettingsModel settingsModel)
        {
            LastError = null;
            try
            {
                if (settingsModel == null || string.IsNullOrWhiteSpace(settingsModel.from) || string.IsNullOrWhiteSpace(settingsModel.host))
                {
                    LastError = "SMTP host/from is not configured.";
                    emailSenderModel.sentStatus = false;
                    emailSenderModel.LastError = LastError;
                    return false;
                }

                if (emailSenderModel == null || string.IsNullOrWhiteSpace(emailSenderModel.ToAddress))
                {
                    LastError = "To address is required.";
                    if (emailSenderModel != null)
                    {
                        emailSenderModel.sentStatus = false;
                        emailSenderModel.LastError = LastError;
                    }
                    return false;
                }

                var displayName = string.IsNullOrWhiteSpace(settingsModel.appName) ? settingsModel.from : settingsModel.appName;
                var fromAddress = new MailAddress(settingsModel.from, displayName);
                var toAddress = new MailAddress(emailSenderModel.ToAddress);
                var smtpUser = string.IsNullOrWhiteSpace(settingsModel.userName) ? settingsModel.from : settingsModel.userName;
                var smtpPassword = (settingsModel.password ?? string.Empty).Replace(" ", string.Empty);
                if (string.IsNullOrWhiteSpace(smtpPassword))
                {
                    LastError = "SMTP password is empty.";
                    emailSenderModel.sentStatus = false;
                    emailSenderModel.LastError = LastError;
                    return false;
                }

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                using (var smtp = new SmtpClient())
                {
                    smtp.Host = settingsModel.host;
                    smtp.Port = settingsModel.port > 0 ? settingsModel.port : 587;
                    smtp.EnableSsl = settingsModel.enableSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Timeout = 30000;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(smtpUser, smtpPassword);

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = emailSenderModel.Subject,
                        Body = emailSenderModel.Body,
                        IsBodyHtml = emailSenderModel.isHtml,
                    })
                    {
                        smtp.Send(message);
                        emailSenderModel.sentStatus = true;
                        emailSenderModel.LastError = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex.GetBaseException().Message;
                emailSenderModel.sentStatus = false;
                emailSenderModel.LastError = LastError;
                Console.Error.WriteLine("SMTP send failed: " + LastError);
            }

            return emailSenderModel.sentStatus;
        }
    }
}
