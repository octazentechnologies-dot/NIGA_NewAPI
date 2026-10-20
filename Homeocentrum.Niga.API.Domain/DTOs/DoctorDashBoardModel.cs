using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DoctorDashBoardModel :PaginationParams
    {
        public int PatientAppId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? Status { get; set; }
        public long UserId { get; set; }
        public int DoctorId { get; set; }
        public int patientApp { get; set; }
        public int walkInpatientApp { get; set; }
        public int patientAppComplated { get; set; }
        public int patientAppWaiting { get; set; }
        public int patientAppNotArrived { get; set; }
        public int patientAppEConsult { get; set; }
        public int patientAppRemaining { get; set; }

        public int patientAppWalkIn { get; set; }

        /// <summary>Total distinct patients registered for this doctor (all time).</summary>
        public int totalPatients { get; set; }

        /// <summary>DOC-01.02 — doctor online flag (nullable until always populated).</summary>
        public bool? IsOnline { get; set; }

        /// <summary>DOC-01.02 — tele / e-consult queue (IsTele or E-Consult status).</summary>
        public int teleQueueCount { get; set; }

        /// <summary>DOC-01.02 — appointments not Paid/Waived (nullable column until Phase 6/11).</summary>
        public int unpaidCount { get; set; }

        /// <summary>Sum of the doctor's consult fee for today's unpaid, non-cancelled visits.</summary>
        public decimal unpaidAmount { get; set; }

        /// <summary>Today's appointments whose status is CANCELLED.</summary>
        public int cancelledCount { get; set; }

        /// <summary>Same value as cancelledCount, for clients that read patientAppCancelled.</summary>
        public int patientAppCancelled { get; set; }

        /// <summary>Same value as unpaidCount.</summary>
        public int unpaidAppointmentCount { get; set; }

        /// <summary>Doctor.FeeCurrency. Null when the profile has no currency.</summary>
        public string? currency { get; set; }
    }

    public class PatientAppModel
    {
        public int PatientAppId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? Status { get; set; }
      
    }
}
