namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class EmailSenderModel
    {
        public string ToAddress { get; set; }
        public string Body { get; set; }
        public bool isHtml { get; set; }
        public string Subject { get; set; }
        public bool sentStatus { get; set; }
        public string LastError { get; set; }
    }
}
