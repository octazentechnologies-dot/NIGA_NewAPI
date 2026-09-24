using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ErrorResponseModel
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
    }
}
