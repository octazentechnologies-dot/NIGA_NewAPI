using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class EnquiryDetail
{
    public int EnquiryId { get; set; }

    public string? EnquiryName { get; set; }

    public DateTime? EnquiryDate { get; set; }

    public string? EmailId { get; set; }

    public string? MobileNo { get; set; }

    public string? EnquiryDetails { get; set; }

    public bool? EnquiryStatus { get; set; }

    public string? TicketStatus { get; set; }

    public long? AssignedTo { get; set; }
}
