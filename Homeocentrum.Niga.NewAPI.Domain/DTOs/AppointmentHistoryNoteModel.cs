using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class AppointmentHistoryNoteModel
    {
        public int HistoryId { get; set; } = 0;
        public int? AppointmentId { get; set; } = 0;
        public string HistoryNote { get; set; }= string.Empty;
    }
}
