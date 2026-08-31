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

                var fromAddress = new MailAddress(settingsModel.from);
                var toAddress = new MailAddress(emailSenderModel.ToAddress);
                var smtp = new SmtpClient
                {
                    Host = settingsModel.host,
                    Port = settingsModel.port,
                    EnableSsl = settingsModel.enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = settingsModel.defaultCredentials,
                    Credentials = new NetworkCredential(fromAddress.Address, settingsModel.password),
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
