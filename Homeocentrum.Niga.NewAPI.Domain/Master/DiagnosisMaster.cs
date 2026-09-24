using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisMaster
{
    public int DiagnosisId { get; set; }

    public int? DiagnosisGroupId { get; set; }

    public string DiagnosisName { get; set; } = null!;

    public string? DiagnosisNameAlias { get; set; }

    public string? Miasm { get; set; }

    public string? Description { get; set; }

    public string? Keywords { get; set; }

    public string? Investigations { get; set; }

    public string? AllopathicMedicines { get; set; }

    public string? Examiniations { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<AccompaniedDetail> AccompaniedDetails { get; set; } = new List<AccompaniedDetail>();

    public virtual ICollection<BeforeAfterDuringDetail> BeforeAfterDuringDetails { get; set; } = new List<BeforeAfterDuringDetail>();

    public virtual ICollection<CaseEntryDiagnosis> CaseEntryDiagnoses { get; set; } = new List<CaseEntryDiagnosis>();

    public virtual ICollection<DiagnosisCausation> DiagnosisCausations { get; set; } = new List<DiagnosisCausation>();

    public virtual ICollection<DiagnosisDetail> DiagnosisDetails { get; set; } = new List<DiagnosisDetail>();

    public virtual ICollection<DiagnosisMonogramDetail> DiagnosisMonogramDetails { get; set; } = new List<DiagnosisMonogramDetail>();

    public virtual ICollection<DiagnosisMonogram> DiagnosisMonograms { get; set; } = new List<DiagnosisMonogram>();

    public virtual ICollection<DiagnosisPathology> DiagnosisPathologies { get; set; } = new List<DiagnosisPathology>();

    public virtual ICollection<DiagnosisPathologyDetail> DiagnosisPathologyDetails { get; set; } = new List<DiagnosisPathologyDetail>();

    public virtual ICollection<DiagnosisSymptom> DiagnosisSymptoms { get; set; } = new List<DiagnosisSymptom>();

    public virtual ICollection<DiagnosisSystemDetail> DiagnosisSystemDetails { get; set; } = new List<DiagnosisSystemDetail>();

    public virtual ICollection<DiagnosisTherapeuticsDetail> DiagnosisTherapeuticsDetails { get; set; } = new List<DiagnosisTherapeuticsDetail>();

    public virtual ICollection<EmergencieDetail> EmergencieDetails { get; set; } = new List<EmergencieDetail>();

    public virtual ICollection<LocationExtentionDetail> LocationExtentionDetails { get; set; } = new List<LocationExtentionDetail>();

    public virtual ICollection<ModalitiesDetail> ModalitiesDetails { get; set; } = new List<ModalitiesDetail>();

    public virtual ICollection<ObservationsDetail> ObservationsDetails { get; set; } = new List<ObservationsDetail>();

    public virtual ICollection<OnsetDurationProgressDetail> OnsetDurationProgressDetails { get; set; } = new List<OnsetDurationProgressDetail>();

    public virtual ICollection<PatternsDetail> PatternsDetails { get; set; } = new List<PatternsDetail>();

    public virtual ICollection<SensationDetail> SensationDetails { get; set; } = new List<SensationDetail>();
}
