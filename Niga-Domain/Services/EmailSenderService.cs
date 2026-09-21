using System;
using System.Net;
using System.Net.Mail;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services
{
    public class EmailSenderService
    {
        public bool SendMail(EmailSenderModel emailSenderModel, SmtpSettingsModel settingsModel)
        {
            try
            {
                if (settingsModel == null || string.IsNullOrWhiteSpace(settingsModel.from) || string.IsNullOrWhiteSpace(settingsModel.host))
                {
                    emailSenderModel.sentStatus = false;
                    return false;
                }

                var displayName = string.IsNullOrWhiteSpace(settingsModel.appName) ? settingsModel.from : settingsModel.appName;
                var fromAddress = new MailAddress(settingsModel.from, displayName);
                var toAddress = new MailAddress(emailSenderModel.ToAddress);
                var smtpUser = string.IsNullOrWhiteSpace(settingsModel.userName) ? settingsModel.from : settingsModel.userName;
                var smtpPassword = (settingsModel.password ?? string.Empty).Replace(" ", string.Empty);
                var smtp = new SmtpClient
                {
                    Host = settingsModel.host,
                    Port = settingsModel.port,
                    EnableSsl = settingsModel.enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = settingsModel.defaultCredentials,
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = emailSenderModel.Subject,
                    Body = emailSenderModel.Body,
                    IsBodyHtml = emailSenderModel.isHtml,
                })
                {
                    smtp.Send(message);
                    emailSenderModel.sentStatus = true;
                }
            }
            catch (Exception)
            {
                emailSenderModel.sentStatus = false;
            }

            return emailSenderModel.sentStatus;
        }
    }
}
