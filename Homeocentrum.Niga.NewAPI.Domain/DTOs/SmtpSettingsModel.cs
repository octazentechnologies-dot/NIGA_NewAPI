using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class SmtpSettingsModel
    {
        public string from { get; set; }
        public string host { get; set; }
        public int port { get; set; }
        public bool defaultCredentials { get; set; }
        public bool enableSsl { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public string appName { get; set; }
        public string SiteUrl { get; set; }
    }
}
