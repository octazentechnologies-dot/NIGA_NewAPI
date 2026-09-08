using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Entities;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
namespace Niga_Domain.Data
{
    public class NIGACentrumContext : DbContext
    {
        public NIGACentrumContext()
        {
        }
        private readonly IDateTime _dateTime;
        private readonly ICurrentUserService _currentUserService;
        IHttpContextAccessor _httpContextAccessor;


        public NIGACentrumContext(DbContextOptions<NIGACentrumContext> options, IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService, IDateTime dateTime)
       : base(options)
        {
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _httpContextAccessor = httpContextAccessor;

        }
        private ClaimsPrincipal GetCurrentPrincipal()
        {
            return Thread.CurrentPrincipal as ClaimsPrincipal;
        }

    public virtual DbSet<AccompaniedDetail> AccompaniedDetails { get; set; }

    public virtual DbSet<AccompaniedRubricDetail> AccompaniedRubricDetails { get; set; }

    public virtual DbSet<AdverseReactionMaster> AdverseReactionMasters { get; set; }

    public virtual DbSet<AllopathicDrugMaster> AllopathicDrugMasters { get; set; }

    public virtual DbSet<AppointmentHistoryNote> AppointmentHistoryNotes { get; set; }

    public virtual DbSet<AuthorMaster> AuthorMasters { get; set; }

    public virtual DbSet<BeforeAfterDuringDetail> BeforeAfterDuringDetails { get; set; }

    public virtual DbSet<BeforeAfterDuringRubricDetail> BeforeAfterDuringRubricDetails { get; set; }

    public virtual DbSet<BlogDetail> BlogDetails { get; set; }

    public virtual DbSet<BodyPartMaster> BodyPartMasters { get; set; }

    public virtual DbSet<BodyPartSectionMaster> BodyPartSectionMasters { get; set; }

    public virtual DbSet<CaseDetail> CaseDetails { get; set; }

    public virtual DbSet<CaseDetailRemedy> CaseDetailRemedies { get; set; }

    public virtual DbSet<CaseEntryChiefComplaint> CaseEntryChiefComplaints { get; set; }

    public virtual DbSet<CaseEntryDetail> CaseEntryDetails { get; set; }

    public virtual DbSet<CaseEntryDiagnosis> CaseEntryDiagnoses { get; set; }

    public virtual DbSet<ChestDataMig> ChestDataMigs { get; set; }

    public virtual DbSet<ChestDataMiglevFive> ChestDataMiglevFives { get; set; }

    public virtual DbSet<Chestdatamigfive> Chestdatamigfives { get; set; }

    public virtual DbSet<Chestdatamigtemp> Chestdatamigtemps { get; set; }

    public virtual DbSet<ClinicalQueKeyword> ClinicalQueKeywords { get; set; }

    public virtual DbSet<ClinicalQueRubric> ClinicalQueRubrics { get; set; }

    public virtual DbSet<ClinicalQuestion> ClinicalQuestions { get; set; }

    public virtual DbSet<ClinicalQuestionBodyPart> ClinicalQuestionBodyParts { get; set; }

    public virtual DbSet<ClipboardRubric> ClipboardRubrics { get; set; }

    public virtual DbSet<CountryMaster> CountryMasters { get; set; }

    public virtual DbSet<DemoDatum> DemoData { get; set; }

    public virtual DbSet<DiagnosisCausation> DiagnosisCausations { get; set; }

    public virtual DbSet<DiagnosisCausationRubricDetail> DiagnosisCausationRubricDetails { get; set; }

    public virtual DbSet<DiagnosisDetail> DiagnosisDetails { get; set; }

    public virtual DbSet<DiagnosisGroupMaster> DiagnosisGroupMasters { get; set; }

    public virtual DbSet<DiagnosisMaster> DiagnosisMasters { get; set; }

    public virtual DbSet<DiagnosisMonogram> DiagnosisMonograms { get; set; }

    public virtual DbSet<DiagnosisMonogramDetail> DiagnosisMonogramDetails { get; set; }

    public virtual DbSet<DiagnosisMonogramRubricDetail> DiagnosisMonogramRubricDetails { get; set; }

    public virtual DbSet<DiagnosisPathology> DiagnosisPathologies { get; set; }

    public virtual DbSet<DiagnosisPathologyDetail> DiagnosisPathologyDetails { get; set; }

    public virtual DbSet<DiagnosisPathologyRubricDetail> DiagnosisPathologyRubricDetails { get; set; }

    public virtual DbSet<DiagnosisSymptom> DiagnosisSymptoms { get; set; }

    public virtual DbSet<DiagnosisSymptomRubric> DiagnosisSymptomRubrics { get; set; }

    public virtual DbSet<DiagnosisSystem> DiagnosisSystems { get; set; }

    public virtual DbSet<DiagnosisSystemDetail> DiagnosisSystemDetails { get; set; }

    public virtual DbSet<DiagnosisTherapeuticsDetail> DiagnosisTherapeuticsDetails { get; set; }

    public virtual DbSet<DiseaseMaster> DiseaseMasters { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<DoctorReceptionStaff> DoctorReceptionStaffs { get; set; }

    public virtual DbSet<DoctorPatientBoardBackup> DoctorPatientBoardBackups { get; set; }

    public virtual DbSet<DoctorDailySchedule> DoctorDailySchedules { get; set; }

    public virtual DbSet<AudioCaseSession> AudioCaseSessions { get; set; }

    public virtual DbSet<AudioCaseSessionEventLog> AudioCaseSessionEventLogs { get; set; }

    public virtual DbSet<AudioCaseAiRequestLog> AudioCaseAiRequestLogs { get; set; }

    public virtual DbSet<AudioCaseConsentLog> AudioCaseConsentLogs { get; set; }

    public virtual DbSet<AudioCaseRubricMatchLog> AudioCaseRubricMatchLogs { get; set; }

    public virtual DbSet<AudioCaseDoctorActionLog> AudioCaseDoctorActionLogs { get; set; }

    public virtual DbSet<AudioCaseRetentionLog> AudioCaseRetentionLogs { get; set; }

    public virtual DbSet<AudioCaseClinicalConcept> AudioCaseClinicalConcepts { get; set; }

    public virtual DbSet<AudioCaseIntelligenceLog> AudioCaseIntelligenceLogs { get; set; }

    public virtual DbSet<RubricMetaphorDictionary> RubricMetaphorDictionaries { get; set; }

    public virtual DbSet<RubricAlias> RubricAliases { get; set; }

    public virtual DbSet<RubricAdminAuditLog> RubricAdminAuditLogs { get; set; }

    public virtual DbSet<HomeopathicWeightRule> HomeopathicWeightRules { get; set; }

    public virtual DbSet<AudioCaseCausationLink> AudioCaseCausationLinks { get; set; }

    public virtual DbSet<RubricEmbedding> RubricEmbeddings { get; set; }

    public virtual DbSet<AudioCaseClinicalInferenceLog> AudioCaseClinicalInferenceLogs { get; set; }

    public virtual DbSet<AudioCaseRubricFeedback> AudioCaseRubricFeedbacks { get; set; }

    public virtual DbSet<AudioCaseRubricBenchmark> AudioCaseRubricBenchmarks { get; set; }

    public virtual DbSet<GoldCaseLibrary> GoldCaseLibraries { get; set; }

    public virtual DbSet<AiPatientMeaning> AiPatientMeanings { get; set; }

    public virtual DbSet<AiMetaphorResolution> AiMetaphorResolutions { get; set; }

    public virtual DbSet<AiSensationOntology> AiSensationOntologies { get; set; }

    public virtual DbSet<AiClinicalConceptV3> AiClinicalConceptsV3 { get; set; }

    public virtual DbSet<AiHomeopathicConcept> AiHomeopathicConcepts { get; set; }

    public virtual DbSet<AiConceptGraphEdge> AiConceptGraphEdges { get; set; }

    public virtual DbSet<AiRubricDiscovery> AiRubricDiscoveries { get; set; }

    public virtual DbSet<AiRubricEvidence> AiRubricEvidences { get; set; }

    public virtual DbSet<AiRubricValidationV3> AiRubricValidationsV3 { get; set; }

    public virtual DbSet<AiRubricConfidence> AiRubricConfidences { get; set; }

    public virtual DbSet<AiDoctorFeedback> AiDoctorFeedbacks { get; set; }

    public virtual DbSet<AiRolloutGate> AiRolloutGates { get; set; }

    public virtual DbSet<AiCaseLearning> AiCaseLearnings { get; set; }

    public virtual DbSet<AiMonitoringDailySnapshot> AiMonitoringDailySnapshots { get; set; }

    public virtual DbSet<AiMonitoringAuditLog> AiMonitoringAuditLogs { get; set; }

    public virtual DbSet<AiKgNode> AiKgNodes { get; set; }

    public virtual DbSet<AiKgEdge> AiKgEdges { get; set; }

    public virtual DbSet<AiKgEdgeEvidence> AiKgEdgeEvidences { get; set; }

    public virtual DbSet<AiKgFeedbackMutation> AiKgFeedbackMutations { get; set; }

    public virtual DbSet<AiKgSessionPath> AiKgSessionPaths { get; set; }

    public virtual DbSet<AiKgRemedyProjection> AiKgRemedyProjections { get; set; }

    public virtual DbSet<AiKgFigurativeResolution> AiKgFigurativeResolutions { get; set; }

    public virtual DbSet<AiReasoningAudit> AiReasoningAudits { get; set; }

    public virtual DbSet<AiConceptMappingBootstrap> AiConceptMappingBootstraps { get; set; }

    public virtual DbSet<AiSymptomBlock> AiSymptomBlocks { get; set; }

    public virtual DbSet<AiConceptCluster> AiConceptClusters { get; set; }

    public virtual DbSet<AiConceptClusterMember> AiConceptClusterMembers { get; set; }

    public virtual DbSet<AiCaseCoverageMetrics> AiCaseCoverageMetrics { get; set; }

    public virtual DbSet<AiMissingSymptomCandidate> AiMissingSymptomCandidates { get; set; }

    public virtual DbSet<AiEmbeddingVersion> AiEmbeddingVersions { get; set; }

    public virtual DbSet<AiRubricEmbedding> AiRubricEmbeddings { get; set; }

    public virtual DbSet<AiConceptEmbedding> AiConceptEmbeddings { get; set; }

    public virtual DbSet<AiEmbeddingJob> AiEmbeddingJobs { get; set; }

    public virtual DbSet<AiEmbeddingQueue> AiEmbeddingQueues { get; set; }

    public virtual DbSet<AiEmbeddingAudit> AiEmbeddingAudits { get; set; }

    public virtual DbSet<AiEmbeddingStatistics> AiEmbeddingStatistics { get; set; }

    public virtual DbSet<AiEmbeddingSyncState> AiEmbeddingSyncStates { get; set; }

    public virtual DbSet<RepertorySource> RepertorySources { get; set; }

    public virtual DbSet<RubricRepertoryMap> RubricRepertoryMaps { get; set; }

    public virtual DbSet<DrugGroupMaster> DrugGroupMasters { get; set; }

    public virtual DbSet<DrugSystemMaster> DrugSystemMasters { get; set; }

    public virtual DbSet<EmergencieDetail> EmergencieDetails { get; set; }

    public virtual DbSet<EmergencieRubricDetail> EmergencieRubricDetails { get; set; }

    public virtual DbSet<EnquiryDetail> EnquiryDetails { get; set; }

    public virtual DbSet<FirmDetail> FirmDetails { get; set; }

    public virtual DbSet<GenderMaster> GenderMasters { get; set; }

    public virtual DbSet<HumanSystemMaster> HumanSystemMasters { get; set; }

    public virtual DbSet<IntensityMaster> IntensityMasters { get; set; }

    public virtual DbSet<LabTestMaster> LabTestMasters { get; set; }

    public virtual DbSet<LanguageMaster> LanguageMasters { get; set; }

    public virtual DbSet<LanguageVersion> LanguageVersions { get; set; }

    public virtual DbSet<LocationExtentionDetail> LocationExtentionDetails { get; set; }

    public virtual DbSet<LocationExtentionRubricDetail> LocationExtentionRubricDetails { get; set; }

    public virtual DbSet<MateriaMedicaDetail> MateriaMedicaDetails { get; set; }

    public virtual DbSet<MateriaMedicaHeadMaster> MateriaMedicaHeadMasters { get; set; }

    public virtual DbSet<MateriaMedicaMaster> MateriaMedicaMasters { get; set; }

    public virtual DbSet<MedicalAstrologyMaster> MedicalAstrologyMasters { get; set; }

    public virtual DbSet<MenuMaster> MenuMasters { get; set; }

    public virtual DbSet<ModalitiesDetail> ModalitiesDetails { get; set; }

    public virtual DbSet<ModalitiesRubricDetail> ModalitiesRubricDetails { get; set; }

    public virtual DbSet<ModuleMaster> ModuleMasters { get; set; }

    public virtual DbSet<Monogram> Monograms { get; set; }

    public virtual DbSet<MonogramDetail> MonogramDetails { get; set; }

    public virtual DbSet<NewsCategory> NewsCategories { get; set; }

    public virtual DbSet<NewsDetail> NewsDetails { get; set; }

    public virtual DbSet<ObservationsDetail> ObservationsDetails { get; set; }

    public virtual DbSet<ObservationsRubricDetail> ObservationsRubricDetails { get; set; }

    public virtual DbSet<OnsetDurationProgressDetail> OnsetDurationProgressDetails { get; set; }

    public virtual DbSet<OnsetDurationProgressRubricDetail> OnsetDurationProgressRubricDetails { get; set; }

    public virtual DbSet<OtherSideEffectMaster> OtherSideEffectMasters { get; set; }

    public virtual DbSet<PackageEntryDetail> PackageEntryDetails { get; set; }

    public virtual DbSet<PackageMaster> PackageMasters { get; set; }

    public virtual DbSet<PackageTopupMaster> PackageTopupMasters { get; set; }

    public virtual DbSet<PartLocationMaster> PartLocationMasters { get; set; }

    public virtual DbSet<Pathology> Pathologies { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientAppointment> PatientAppointments { get; set; }

    public virtual DbSet<PatientLabEntry> PatientLabEntries { get; set; }

    public virtual DbSet<PatientLabOrder> PatientLabOrders { get; set; }

    public virtual DbSet<PatientLabTestMaster> PatientLabTestMasters { get; set; }

    public virtual DbSet<PatternRubricDetail> PatternRubricDetails { get; set; }

    public virtual DbSet<PatternsDetail> PatternsDetails { get; set; }

    public virtual DbSet<PrescriptionRemedyDetail> PrescriptionRemedyDetails { get; set; }

    public virtual DbSet<PrescriptionRubricDetail> PrescriptionRubricDetails { get; set; }

    public virtual DbSet<PsChangeDate> PsChangeDates { get; set; }

    public virtual DbSet<QualificationMaster> QualificationMasters { get; set; }

    public virtual DbSet<QuestionGroupMaster> QuestionGroupMasters { get; set; }

    public virtual DbSet<QuestionSectionMaster> QuestionSectionMasters { get; set; }

    public virtual DbSet<QuestionSubgroup> QuestionSubgroups { get; set; }

    public virtual DbSet<ReferenceRubricDetail> ReferenceRubricDetails { get; set; }

    public virtual DbSet<RemedyGradeMaster> RemedyGradeMaster { get; set; }

    public virtual DbSet<RemedyMaster> RemedyMasters { get; set; }

    public virtual DbSet<RemedyRubricAuthorDetail> RemedyRubricAuthorDetails { get; set; }

    public virtual DbSet<ReportSetting> ReportSettings { get; set; }

    public virtual DbSet<RoleDetail> RoleDetails { get; set; }

    public virtual DbSet<RoleMaster> RoleMasters { get; set; }

    public virtual DbSet<RubricRemedyDetail> RubricRemedyDetails { get; set; }

    public virtual DbSet<SampleTable> SampleTables { get; set; }

    public virtual DbSet<SearchPageSetting> SearchPageSettings { get; set; }

    public virtual DbSet<SectionGroupMaster> SectionGroupMasters { get; set; }

    public virtual DbSet<SectionMaster> SectionMasters { get; set; }

    public virtual DbSet<SensationDetail> SensationDetails { get; set; }

    public virtual DbSet<SensationRubricDetail> SensationRubricDetails { get; set; }

    public virtual DbSet<SeriousSideEffectMaster> SeriousSideEffectMasters { get; set; }

    public virtual DbSet<StateMaster> StateMasters { get; set; }

    public virtual DbSet<SubSectionLanguageDetail> SubSectionLanguageDetails { get; set; }

    public virtual DbSet<SubSectionMaster> SubSectionMasters { get; set; }

    public virtual DbSet<ThermalMaster> ThermalMasters { get; set; }

    public virtual DbSet<ThreeDBodyPartMeshKeyMaster> ThreeDBodyPartMeshKeyMasters { get; set; }

    public virtual DbSet<ThreeDBodyPartSectionMaster> ThreeDBodyPartSectionMasters { get; set; }

    public virtual DbSet<ThreeDBodyPartSectionHotspot> ThreeDBodyPartSectionHotspots { get; set; }

    public virtual DbSet<TypeofSymptomsGroupMaster> TypeofSymptomsGroupMasters { get; set; }

    public virtual DbSet<TypeofSymptomsMaster> TypeofSymptomsMasters { get; set; }

    public virtual DbSet<UserDetail> UserDetails { get; set; }

    public virtual DbSet<UserLoginStatus> UserLoginStatuses { get; set; }

    public virtual DbSet<UserMaster> UserMasters { get; set; }

    // M01 Foundation Security (SEC-02 … SEC-09) — tables via M01_Foundation_Security_CreateTables.sql
    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<ConsentType> ConsentTypes { get; set; }

    public virtual DbSet<ConsentRecord> ConsentRecords { get; set; }

    public virtual DbSet<OtpChallenge> OtpChallenges { get; set; }

    public virtual DbSet<OtpAuditLog> OtpAuditLogs { get; set; }

    public virtual DbSet<AuditEvent> AuditEvents { get; set; }

    public virtual DbSet<SecureDocument> SecureDocuments { get; set; }

    public virtual DbSet<WhatsAppMessageLog> WhatsAppMessageLogs { get; set; }

    public virtual DbSet<WhatsAppTemplateMaster> WhatsAppTemplateMasters { get; set; }

    public virtual DbSet<WhatsAppCampaign> WhatsAppCampaigns { get; set; }

    public virtual DbSet<YearMaster> YearMasters { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=103.154.184.104;Database=HomeoCentrum_Production;User Id=sa;Password=Homeo@niga19;TrustServerCertificate=true;");



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=103.196.187.99,1433;Database=HomeoCentrum_Dev;User Id=sa;Password=nik@123JAM;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccompaniedDetail>(entity =>
        {
            entity.HasKey(e => e.AccompaniedDetailsId);

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.AccompaniedDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AccompaniedDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<AccompaniedRubricDetail>(entity =>
        {
            entity.HasKey(e => e.AccompaniedRubricDetailsId);

            entity.HasOne(d => d.AccompaniedDetails).WithMany(p => p.AccompaniedRubricDetails)
                .HasForeignKey(d => d.AccompaniedDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AccompaniedRubricDetails_AccompaniedDetails");
        });

        modelBuilder.Entity<AdverseReactionMaster>(entity =>
        {
            entity.HasKey(e => e.AdverseReactionId);

            entity.ToTable("AdverseReactionMaster");

            entity.HasOne(d => d.AllopathicDrug).WithMany(p => p.AdverseReactionMasters)
                .HasForeignKey(d => d.AllopathicDrugId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdverseReactionMaster_AllopathicDrugMaster");
        });

        modelBuilder.Entity<AllopathicDrugMaster>(entity =>
        {
            entity.HasKey(e => e.AllopathicDrugId);

            entity.ToTable("AllopathicDrugMaster");

            entity.HasOne(d => d.DrugGroup).WithMany(p => p.AllopathicDrugMasters)
                .HasForeignKey(d => d.DrugGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllopathicDrugMaster_DrugGroupMaster");
        });

        modelBuilder.Entity<AppointmentHistoryNote>(entity =>
        {
            entity.HasKey(e => e.HistoryId);

            entity.ToTable("AppointmentHistoryNote");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifyDate).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentHistoryNotes)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK_AppointmentHistoryNote_PatientAppointment");
        });

        modelBuilder.Entity<AuthorMaster>(entity =>
        {
            entity.HasKey(e => e.AuthorId);

            entity.ToTable("AuthorMaster");

            entity.Property(e => e.AuthorAlias).HasMaxLength(20);
            entity.Property(e => e.AuthorName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<BeforeAfterDuringDetail>(entity =>
        {
            entity.HasKey(e => e.BeforeAfterDuringDetailsId);

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.BeforeAfterDuringDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BeforeAfterDuringDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<BeforeAfterDuringRubricDetail>(entity =>
        {
            entity.HasKey(e => e.BeforeAfterDuringRubricDetailsId);

            entity.HasOne(d => d.BeforeAfterDuringDetails).WithMany(p => p.BeforeAfterDuringRubricDetails)
                .HasForeignKey(d => d.BeforeAfterDuringDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BeforeAfterDuringRubricDetails_BeforeAfterDuringDetails");
        });

        modelBuilder.Entity<BlogDetail>(entity =>
        {
            entity.HasKey(e => e.BlogId);

            entity.Property(e => e.BlogDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<BodyPartMaster>(entity =>
        {
            entity.HasKey(e => e.BodyPartId);

            entity.ToTable("BodyPartMaster");

            entity.Property(e => e.BodyPartName).HasMaxLength(500);
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");

            entity.HasOne(d => d.Section).WithMany(p => p.BodyPartMasters)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BodyPartMaster_BodyPartSectionMaster");
        });

        modelBuilder.Entity<BodyPartSectionMaster>(entity =>
        {
            entity.HasKey(e => e.BodyPartSectionId);

            entity.ToTable("BodyPartSectionMaster");

            entity.Property(e => e.BodyPartSectionName).HasMaxLength(500);
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<CaseDetail>(entity =>
        {
            entity.HasOne(d => d.Intensity).WithMany(p => p.CaseDetails)
                .HasForeignKey(d => d.IntensityId)
                .HasConstraintName("FK_CaseDetails_IntensityMaster");

            entity.HasOne(d => d.Subsection).WithMany(p => p.CaseDetails)
                .HasForeignKey(d => d.SubsectionId)
                .HasConstraintName("FK_CaseDetails_SubSectionMaster");
        });

        modelBuilder.Entity<CaseDetailRemedy>(entity =>
        {
            entity.ToTable("CaseDetailRemedy");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseDetailRemedies)
                .HasForeignKey(d => d.CaseId)
                .HasConstraintName("FK_CaseDetailRemedy_CaseEntryDetails");
        });

        modelBuilder.Entity<CaseEntryChiefComplaint>(entity =>
        {
            entity.HasKey(e => e.CaseChiefComplaintId);

            entity.ToTable("CaseEntryChiefComplaint");

            entity.Property(e => e.ChiefComplaintName).HasMaxLength(500);

            entity.HasOne(d => d.Case).WithMany(p => p.CaseEntryChiefComplaints)
                .HasForeignKey(d => d.CaseId)
                .HasConstraintName("FK_CaseEntryChiefComplaint_CaseEntryDetails");
        });

        modelBuilder.Entity<CaseEntryDetail>(entity =>
        {
            entity.HasKey(e => e.CaseId);

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DateodFirstVisit).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.RefBy).HasMaxLength(50);

            entity.HasOne(d => d.Doctor).WithMany(p => p.CaseEntryDetails)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CaseEntryDetails_Doctor");

            entity.HasOne(d => d.Patient).WithMany(p => p.CaseEntryDetails)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CaseEntryDetails_Patient");
        });

        modelBuilder.Entity<CaseEntryDiagnosis>(entity =>
        {
            entity.HasKey(e => e.CaseDiagnosisId);

            entity.ToTable("CaseEntryDiagnosis");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseEntryDiagnoses)
                .HasForeignKey(d => d.CaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CaseEntryDiagnosis_CaseEntryDiagnosis");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.CaseEntryDiagnoses)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CaseEntryDiagnosis_DiagnosisMaster");
        });

        modelBuilder.Entity<ChestDataMig>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ChestDataMig");

            entity.Property(e => e.IdsymptomwithBodyPart)
                .HasMaxLength(255)
                .HasColumnName("IDSymptomwithBodyPart");
            entity.Property(e => e.ParentIdforSymptomswithLocation).HasColumnName("ParentIDforSymptomswithLocation");
            entity.Property(e => e.Section).HasMaxLength(255);
            entity.Property(e => e.SymptomlevelSeven).HasMaxLength(255);
            entity.Property(e => e.SymptomlevelSix).HasMaxLength(255);
            entity.Property(e => e.Symptomlevelfive).HasMaxLength(255);
            entity.Property(e => e.SymptomswithLocation).HasMaxLength(255);
            entity.Property(e => e.SymptomwithBodyPart).HasMaxLength(255);
            entity.Property(e => e.TypeofSymptoms).HasMaxLength(255);
        });

        modelBuilder.Entity<ChestDataMiglevFive>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ChestDataMiglevFive");

            entity.Property(e => e.IdsymptomswithLocation)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("IDSymptomswithLocation");
            entity.Property(e => e.IdsymptomwithBodyPart).HasColumnName("IDSymptomwithBodyPart");
            entity.Property(e => e.ParantFive).HasMaxLength(255);
            entity.Property(e => e.Section).HasMaxLength(255);
            entity.Property(e => e.SymptomlevelSeven).HasMaxLength(255);
            entity.Property(e => e.SymptomlevelSix).HasMaxLength(255);
            entity.Property(e => e.Symptomlevelfive).HasMaxLength(255);
            entity.Property(e => e.SymptomswithLocation).HasMaxLength(255);
            entity.Property(e => e.SymptomwithBodyPart).HasMaxLength(255);
            entity.Property(e => e.TypeofSymptoms).HasMaxLength(255);
        });

        modelBuilder.Entity<Chestdatamigfive>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("chestdatamigfive");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Subsectionlevelfive).HasColumnName("subsectionlevelfive");
        });

        modelBuilder.Entity<Chestdatamigtemp>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("chestdatamigtemp");

            entity.Property(e => e.Id).HasColumnName("id");
        });

        modelBuilder.Entity<ClinicalQueKeyword>(entity =>
        {
            entity.HasOne(d => d.Questions).WithMany(p => p.ClinicalQueKeywords)
                .HasForeignKey(d => d.QuestionsId)
                .HasConstraintName("FK_ClinicalQueKeywords_ClinicalQuestions");
        });

        modelBuilder.Entity<ClinicalQueRubric>(entity =>
        {
            entity.Property(e => e.ClinicalQuestionBodyPartId).HasColumnName("ClinicalQuestionBodyPartID");

            entity.HasOne(d => d.Subsection).WithMany(p => p.ClinicalQueRubrics)
                .HasForeignKey(d => d.SubsectionId)
                .HasConstraintName("FK_ClinicalQueRubrics_SubSectionMaster");
        });

        modelBuilder.Entity<ClinicalQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestionsId);

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");

            entity.HasOne(d => d.QuestionGroup).WithMany(p => p.ClinicalQuestions)
                .HasForeignKey(d => d.QuestionGroupId)
                .HasConstraintName("FK_ClinicalQuestions_QuestionGroupMaster");
        });

        modelBuilder.Entity<ClinicalQuestionBodyPart>(entity =>
        {
            entity.ToTable("ClinicalQuestionBodyPart");

            entity.Property(e => e.ClinicalQuestionBodyPartId).HasColumnName("ClinicalQuestionBodyPartID");
        });

        modelBuilder.Entity<ClipboardRubric>(entity =>
        {
            entity.HasKey(e => e.ClipboardRubricsId);

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.Intensity)
                .HasMaxLength(10)
                .IsFixedLength();

            entity.HasOne(d => d.SubSection).WithMany(p => p.ClipboardRubrics)
                .HasForeignKey(d => d.SubSectionId)
                .HasConstraintName("FK_ClipboardRubrics_SubSectionMaster");
        });

        modelBuilder.Entity<CountryMaster>(entity =>
        {
            entity.HasKey(e => e.CountryId);

            entity.ToTable("CountryMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.CountryCode).HasMaxLength(50);
            entity.Property(e => e.CountryName).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<DemoDatum>(entity =>
        {
            entity.HasKey(e => e.SubSectionLanguageId);

            entity.HasOne(d => d.Language).WithMany(p => p.DemoData)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DemoData_LanguageMaster");

            entity.HasOne(d => d.SubSection).WithMany(p => p.DemoData)
                .HasForeignKey(d => d.SubSectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DemoData_SubSectionMaster");
        });

        modelBuilder.Entity<DiagnosisCausation>(entity =>
        {
            entity.HasKey(e => e.CausationId);

            entity.ToTable("DiagnosisCausation");

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisCausations)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_DiagnosisCausation_DiagnosisMaster");
        });

        modelBuilder.Entity<DiagnosisCausationRubricDetail>(entity =>
        {
            entity.HasKey(e => e.CausationRubricDetailsId).HasName("PK_CausationRubricDetails");

            entity.HasOne(d => d.Causation).WithMany(p => p.DiagnosisCausationRubricDetails)
                .HasForeignKey(d => d.CausationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CausationRubricDetails_DiagnosisCausation");
        });

        modelBuilder.Entity<DiagnosisDetail>(entity =>
        {
            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_DiagnosisDetails_DiagnosisMaster");

            entity.HasOne(d => d.SubSection).WithMany(p => p.DiagnosisDetails)
                .HasForeignKey(d => d.SubSectionId)
                .HasConstraintName("FK_DiagnosisDetails_SubSectionMaster");
        });

        modelBuilder.Entity<DiagnosisGroupMaster>(entity =>
        {
            entity.HasKey(e => e.DiagnosisGroupId);

            entity.ToTable("DiagnosisGroupMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.DiagnosisGroupName).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<DiagnosisMaster>(entity =>
        {
            entity.HasKey(e => e.DiagnosisId);

            entity.ToTable("DiagnosisMaster");

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.DiagnosisName).HasMaxLength(200);
            entity.Property(e => e.DiagnosisNameAlias).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.Keywords).HasMaxLength(100);
        });

        modelBuilder.Entity<DiagnosisMonogram>(entity =>
        {
            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisMonograms)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_DiagnosisMonograms_DiagnosisMaster");

            entity.HasOne(d => d.Monogram).WithMany(p => p.DiagnosisMonograms)
                .HasForeignKey(d => d.MonogramId)
                .HasConstraintName("FK_DiagnosisMonograms_Monogram");
        });

        modelBuilder.Entity<DiagnosisMonogramDetail>(entity =>
        {
            entity.HasKey(e => e.DiagnosisMonogramDetailsId);

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisMonogramDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisMonogramDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<DiagnosisMonogramRubricDetail>(entity =>
        {
            entity.HasKey(e => e.DiagnosisMonogramRubricDetailsId);

            entity.HasOne(d => d.DiagnosisMonogramDetails).WithMany(p => p.DiagnosisMonogramRubricDetails)
                .HasForeignKey(d => d.DiagnosisMonogramDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisMonogramRubricDetails_DiagnosisMonogramDetails");
        });

        modelBuilder.Entity<DiagnosisPathology>(entity =>
        {
            entity.ToTable("DiagnosisPathology");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisPathologies)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_DiagnosisPathology_DiagnosisMaster");

            entity.HasOne(d => d.Pathology).WithMany(p => p.DiagnosisPathologies)
                .HasForeignKey(d => d.PathologyId)
                .HasConstraintName("FK_DiagnosisPathology_Pathology");
        });

        modelBuilder.Entity<DiagnosisPathologyDetail>(entity =>
        {
            entity.HasKey(e => e.DiagnosisPathologyDetailsId);

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisPathologyDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisPathologyDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<DiagnosisPathologyRubricDetail>(entity =>
        {
            entity.HasKey(e => e.DiagnosisPathologyRubricDetailsId);

            entity.HasOne(d => d.DiagnosisPathologyDetails).WithMany(p => p.DiagnosisPathologyRubricDetails)
                .HasForeignKey(d => d.DiagnosisPathologyDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisPathologyRubricDetails_DiagnosisPathologyDetails");
        });

        modelBuilder.Entity<DiagnosisSymptom>(entity =>
        {
            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisSymptoms)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_DiagnosisSymptoms_DiagnosisMaster");
        });

        modelBuilder.Entity<DiagnosisSymptomRubric>(entity =>
        {
            entity.ToTable("DiagnosisSymptomRubric");

            entity.HasOne(d => d.DiagnosisSymptom).WithMany(p => p.DiagnosisSymptomRubrics)
                .HasForeignKey(d => d.DiagnosisSymptomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisSymptomRubric_DiagnosisSymptoms");
        });

        modelBuilder.Entity<DiagnosisSystem>(entity =>
        {
            entity.ToTable("DiagnosisSystem");
        });

        modelBuilder.Entity<DiagnosisSystemDetail>(entity =>
        {
            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisSystemDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisSystemDetails_DiagnosisMaster");

            entity.HasOne(d => d.DiagnosisSystem).WithMany(p => p.DiagnosisSystemDetails)
                .HasForeignKey(d => d.DiagnosisSystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisSystemDetails_DiagnosisSystem");
        });

        modelBuilder.Entity<DiagnosisTherapeuticsDetail>(entity =>
        {
            entity.ToTable("DiagnosisTherapeuticsDetail");

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");
            entity.Property(e => e.DiagnosisTherapeuticsDetail1).HasColumnName("DiagnosisTherapeuticsDetail");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.DiagnosisTherapeuticsDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DiagnosisTherapeuticsDetail_DiagnosisTherapeuticsDetail");
        });

        modelBuilder.Entity<DiseaseMaster>(entity =>
        {
            entity.HasKey(e => e.DiseaseId);

            entity.ToTable("DiseaseMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK_DoctorMaster");

            entity.ToTable("Doctor");

            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.EmailId).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.MiddleName).HasMaxLength(50);
            entity.Property(e => e.MobileNo).HasMaxLength(15);
            entity.Property(e => e.PassingCertNo).HasMaxLength(50);
            entity.Property(e => e.PassingUniversity).HasMaxLength(500);
            entity.Property(e => e.PermanantAddress).HasMaxLength(500);
            entity.Property(e => e.QualificationId).HasColumnName("QualificationID");

            entity.HasOne(d => d.Package).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_DoctorMaster_PackageMaster");

            entity.HasOne(d => d.Qualification).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.QualificationId)
                .HasConstraintName("FK_DoctorMaster_QualificationMaster");
        });

        modelBuilder.Entity<DoctorDailySchedule>(entity =>
        {
            entity.HasKey(e => e.DoctorDailyScheduleId);

            entity.ToTable("DoctorDailySchedule");

            entity.HasIndex(e => new { e.DoctorId, e.ScheduleDate }, "UQ_DoctorDailySchedule_Doctor_Date")
                .IsUnique();

            entity.Property(e => e.ScheduleDate).HasColumnType("date");
            entity.Property(e => e.WorkStartTime).HasColumnType("time(0)");
            entity.Property(e => e.WorkEndTime).HasColumnType("time(0)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2(0)");

            entity.HasOne(d => d.Doctor).WithMany()
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DoctorDailySchedule_Doctor");
        });

        modelBuilder.Entity<DoctorReceptionStaff>(entity =>
        {
            entity.HasKey(e => e.ReceptionStaffId).HasName("PK_DoctorReceptionStaff");

            entity.ToTable("DoctorReceptionStaff");

            entity.HasIndex(e => e.DoctorId, "IX_DoctorReceptionStaff_DoctorID");
            entity.HasIndex(e => e.UserId, "IX_DoctorReceptionStaff_UserID");
            entity.HasIndex(e => e.ContactNumber, "IX_DoctorReceptionStaff_ContactNumber");
            entity.HasIndex(e => e.EmailId, "IX_DoctorReceptionStaff_EmailId");

            entity.Property(e => e.ReceptionStaffId).HasColumnName("ReceptionStaffID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.UserId)
                .HasColumnName("UserID")
                .HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(500);
            entity.Property(e => e.FullName).HasMaxLength(250);
            entity.Property(e => e.ContactNumber).HasMaxLength(50);
            entity.Property(e => e.EmailId).HasMaxLength(250);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DeleteStatus)
                .HasDefaultValue(false);

            entity.HasOne(d => d.Doctor).WithMany(p => p.DoctorReceptionStaffs)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DoctorReceptionStaff_Doctor");
        });

        modelBuilder.Entity<DoctorPatientBoardBackup>(entity =>
        {
            entity.HasKey(e => e.BackupId).HasName("PK_DoctorPatientBoardBackup");

            entity.ToTable("DoctorPatientBoardBackup");

            entity.HasIndex(e => e.DoctorUserId, "UX_DoctorPatientBoardBackup_DoctorUserId_Active")
                .IsUnique()
                .HasFilter("([DeleteStatus]=(0))");

            entity.Property(e => e.BackupId).HasColumnName("BackupID");
            entity.Property(e => e.DoctorUserId).HasColumnName("DoctorUserID");
            entity.Property(e => e.BackupPayload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DeleteStatus).HasDefaultValue(false);
            entity.Property(e => e.SchemaVersion).HasDefaultValue(1);
            entity.Property(e => e.PatientCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<AudioCaseSession>(entity =>
        {
            entity.HasKey(e => e.AudioCaseSessionId).HasName("PK_AudioCaseSession");
            entity.ToTable("AudioCaseSession");
            entity.Property(e => e.AudioCaseSessionId).HasColumnName("AudioCaseSessionId");
            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.CaseId).HasColumnName("CaseId");
            entity.Property(e => e.DoctorUserId).HasColumnName("DoctorUserId");
            entity.Property(e => e.PatientAppId).HasColumnName("PatientAppId");
            entity.Property(e => e.AudioSourceType).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.CurrentStep).HasMaxLength(50);
            entity.Property(e => e.AudioFilePath).HasMaxLength(500);
            entity.Property(e => e.AudioFileName).HasMaxLength(255);
            entity.Property(e => e.AudioMimeType).HasMaxLength(100);
            entity.Property(e => e.AudioSha256Hash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.DetectedLanguage).HasMaxLength(10);
            entity.Property(e => e.LanguageOverride).HasMaxLength(10);
            entity.Property(e => e.CorrelationId).HasMaxLength(50);
            entity.Property(e => e.ErrorCode).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("datetime");
            entity.Property(e => e.AudioPurgedAtUtc).HasColumnType("datetime");
            entity.Property(e => e.ClinicalConceptsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CausationLinksJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IntelligenceEngineVersion).HasMaxLength(10);
            entity.Property(e => e.ConceptGraphEngineVersion).HasMaxLength(10);
            entity.Property(e => e.DeleteStatus).HasDefaultValue(false);
        });

        modelBuilder.Entity<AudioCaseClinicalConcept>(entity =>
        {
            entity.HasKey(e => e.ConceptId).HasName("PK_AudioCaseClinicalConcept");
            entity.ToTable("AudioCaseClinicalConcept");
            entity.Property(e => e.RawStatement).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.SourceLanguage).HasMaxLength(10);
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseClinicalConcept_Session");
        });

        modelBuilder.Entity<AudioCaseIntelligenceLog>(entity =>
        {
            entity.HasKey(e => e.IntelligenceLogId).HasName("PK_AudioCaseIntelligenceLog");
            entity.ToTable("AudioCaseIntelligenceLog");
            entity.Property(e => e.CorrelationId).HasMaxLength(32);
            entity.Property(e => e.StageName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.EngineVersion).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseIntelligenceLog_Session");
        });

        modelBuilder.Entity<RubricMetaphorDictionary>(entity =>
        {
            entity.HasKey(e => e.MetaphorId).HasName("PK_RubricMetaphorDictionary");
            entity.ToTable("RubricMetaphorDictionary");
            entity.Property(e => e.PatientExpression).HasMaxLength(500);
            entity.Property(e => e.NormalizedExpression).HasMaxLength(500);
            entity.Property(e => e.ClinicalMeaning).HasMaxLength(1000);
            entity.Property(e => e.RubricMeaning).HasMaxLength(500);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.ApprovalStatus).HasMaxLength(20);
            entity.Property(e => e.ConfidenceWeight).HasColumnType("decimal(5,4)");
            entity.Property(e => e.AcceptanceRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ApprovedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<RubricAlias>(entity =>
        {
            entity.HasKey(e => e.RubricAliasId).HasName("PK_RubricAlias");
            entity.ToTable("RubricAlias");
            entity.Property(e => e.AliasText).HasMaxLength(500);
            entity.Property(e => e.NormalizedAlias).HasMaxLength(500);
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.AliasType).HasMaxLength(50);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.Weight).HasColumnType("decimal(5,4)");
            entity.Property(e => e.AcceptanceRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.HasOne<SubSectionMaster>()
                .WithMany()
                .HasForeignKey(e => e.SubSectionId)
                .HasConstraintName("FK_RubricAlias_SubSection");
        });

        modelBuilder.Entity<RubricAdminAuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK_RubricAdminAuditLog");
            entity.ToTable("RubricAdminAuditLog");
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.ActionType).HasMaxLength(30);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<HomeopathicWeightRule>(entity =>
        {
            entity.HasKey(e => e.WeightRuleId).HasName("PK_HomeopathicWeightRule");
            entity.ToTable("HomeopathicWeightRule");
            entity.Property(e => e.RuleCode).HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.WeightValue).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MultiplierValue).HasColumnType("decimal(6,3)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AudioCaseCausationLink>(entity =>
        {
            entity.HasKey(e => e.CausationLinkId).HasName("PK_AudioCaseCausationLink");
            entity.ToTable("AudioCaseCausationLink");
            entity.Property(e => e.CauseText).HasMaxLength(500);
            entity.Property(e => e.EffectText).HasMaxLength(500);
            entity.Property(e => e.LinkType).HasMaxLength(30);
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseCausationLink_Session");
        });

        modelBuilder.Entity<RubricEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_RubricEmbeddings");
            entity.ToTable("RubricEmbeddings");
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.TextHash).HasMaxLength(64);
            entity.Property(e => e.SourceType).HasMaxLength(30);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
            entity.HasOne<SubSectionMaster>()
                .WithMany()
                .HasForeignKey(e => e.RubricId)
                .HasConstraintName("FK_RubricEmbeddings_SubSection");
        });

        modelBuilder.Entity<AudioCaseSessionEventLog>(entity =>
        {
            entity.HasKey(e => e.EventLogId).HasName("PK_AudioCaseSessionEventLog");
            entity.ToTable("AudioCaseSessionEventLog");
            entity.Property(e => e.EventType).HasMaxLength(80);
            entity.Property(e => e.EventStatus).HasMaxLength(20);
            entity.Property(e => e.CorrelationId).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseSessionEventLog_Session");
        });

        modelBuilder.Entity<AudioCaseAiRequestLog>(entity =>
        {
            entity.HasKey(e => e.AiRequestLogId).HasName("PK_AudioCaseAiRequestLog");
            entity.ToTable("AudioCaseAiRequestLog");
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.ServiceType).HasMaxLength(50);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.RequestId).HasMaxLength(100);
            entity.Property(e => e.RequestPayloadHash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.ResponsePayloadHash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.ErrorCode).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.CorrelationId).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseAiRequestLog_Session");
        });

        modelBuilder.Entity<AudioCaseConsentLog>(entity =>
        {
            entity.HasKey(e => e.ConsentLogId).HasName("PK_AudioCaseConsentLog");
            entity.ToTable("AudioCaseConsentLog");
            entity.Property(e => e.ConsentType).HasMaxLength(50);
            entity.Property(e => e.ConsentTextVersion).HasMaxLength(20);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseConsentLog_Session");
        });

        modelBuilder.Entity<AudioCaseRubricMatchLog>(entity =>
        {
            entity.HasKey(e => e.RubricMatchLogId).HasName("PK_AudioCaseRubricMatchLog");
            entity.ToTable("AudioCaseRubricMatchLog");
            entity.Property(e => e.SymptomPhrase).HasMaxLength(500);
            entity.Property(e => e.SubSectionName).HasMaxLength(500);
            entity.Property(e => e.MatchSource).HasMaxLength(50);
            entity.Property(e => e.ClinicalMeaning).HasMaxLength(1000);
            entity.Property(e => e.RubricTier).HasMaxLength(30);
            entity.Property(e => e.MatchLayer).HasMaxLength(50);
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.HomeopathicWeight).HasColumnType("decimal(5,2)");
            entity.Property(e => e.UnifiedSource).HasMaxLength(30);
            entity.Property(e => e.FinalHybridScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseRubricMatchLog_Session");
        });

        modelBuilder.Entity<AudioCaseClinicalInferenceLog>(entity =>
        {
            entity.HasKey(e => e.InferenceLogId).HasName("PK_AudioCaseClinicalInferenceLog");
            entity.ToTable("AudioCaseClinicalInferenceLog");
            entity.Property(e => e.InferredRubricName).HasMaxLength(500);
            entity.Property(e => e.SourceSymptom).HasMaxLength(500);
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseClinicalInferenceLog_Session");
        });

        modelBuilder.Entity<AudioCaseRubricFeedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK_AudioCaseRubricFeedback");
            entity.ToTable("AudioCaseRubricFeedback");
            entity.Property(e => e.RubricName).HasMaxLength(500);
            entity.Property(e => e.FeedbackType).HasMaxLength(30);
            entity.Property(e => e.OriginalMatchLayer).HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.RejectReasonStage).HasMaxLength(40);
            entity.Property(e => e.RejectReasonNote).HasMaxLength(1000);
            entity.Property(e => e.EngineVersion).HasMaxLength(10);
            entity.Property(e => e.ConfidenceAtFeedback).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseRubricFeedback_Session");
        });

        modelBuilder.Entity<AudioCaseRubricBenchmark>(entity =>
        {
            entity.HasKey(e => e.BenchmarkId).HasName("PK_AudioCaseRubricBenchmark");
            entity.ToTable("AudioCaseRubricBenchmark");
            entity.Property(e => e.EngineVersion).HasMaxLength(10);
            entity.Property(e => e.PrecisionScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.RecallScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.F1Score).HasColumnType("decimal(5,4)");
            entity.Property(e => e.AcceptanceRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.FalsePositiveRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.ConfidenceCalibration).HasColumnType("decimal(5,4)");
            entity.Property(e => e.CalculatedDate).HasColumnType("datetime");
            entity.HasIndex(e => e.AudioCaseSessionId).IsUnique().HasDatabaseName("IX_AudioCaseRubricBenchmark_Session");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseRubricBenchmark_Session");
        });

        modelBuilder.Entity<GoldCaseLibrary>(entity =>
        {
            entity.HasKey(e => e.GoldCaseId).HasName("PK_GoldCaseLibrary");
            entity.ToTable("GoldCaseLibrary");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.SourceLanguage).HasMaxLength(10);
            entity.Property(e => e.FinalRemedy).HasMaxLength(200);
            entity.Property(e => e.FollowUpOutcome).HasMaxLength(1000);
            entity.Property(e => e.ReviewDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AiPatientMeaning>(entity =>
        {
            entity.HasKey(e => e.PatientMeaningId).HasName("PK_AIPatientMeaning");
            entity.ToTable("AIPatientMeaning");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
            entity.HasOne<AudioCaseSession>().WithMany().HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AIPatientMeaning_Session");
        });

        modelBuilder.Entity<AiReasoningAudit>(entity =>
        {
            entity.HasKey(e => e.ReasoningAuditId).HasName("PK_AIReasoningAudit");
            entity.ToTable("AIReasoningAudit");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
            entity.HasOne<AudioCaseSession>().WithMany().HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AIReasoningAudit_Session");
        });

        modelBuilder.Entity<AiMetaphorResolution>(entity =>
        {
            entity.HasKey(e => e.MetaphorResolutionId).HasName("PK_AIMetaphorResolution");
            entity.ToTable("AIMetaphorResolution");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiSensationOntology>(entity =>
        {
            entity.HasKey(e => e.OntologyId).HasName("PK_AISensationOntology");
            entity.ToTable("AISensationOntology");
            entity.Property(e => e.ExtractedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiClinicalConceptV3>(entity =>
        {
            entity.HasKey(e => e.ClinicalConceptId).HasName("PK_AIClinicalConcept");
            entity.ToTable("AIClinicalConcept");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiHomeopathicConcept>(entity =>
        {
            entity.HasKey(e => e.HomeopathicConceptId).HasName("PK_AIHomeopathicConcept");
            entity.ToTable("AIHomeopathicConcept");
            entity.Property(e => e.Weight).HasColumnType("decimal(6,3)");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiConceptGraphEdge>(entity =>
        {
            entity.HasKey(e => e.ConceptGraphEdgeId).HasName("PK_AIConceptGraph");
            entity.ToTable("AIConceptGraph");
            entity.Property(e => e.Weight).HasColumnType("decimal(6,3)");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiRubricDiscovery>(entity =>
        {
            entity.HasKey(e => e.RubricDiscoveryId).HasName("PK_AIRubricDiscovery");
            entity.ToTable("AIRubricDiscovery");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiRubricEvidence>(entity =>
        {
            entity.HasKey(e => e.RubricEvidenceId).HasName("PK_AIRubricEvidence");
            entity.ToTable("AIRubricEvidence");
            entity.Property(e => e.CoverageScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiRubricValidationV3>(entity =>
        {
            entity.HasKey(e => e.RubricValidationId).HasName("PK_AIRubricValidation");
            entity.ToTable("AIRubricValidation");
            entity.Property(e => e.QualityScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ValidatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiRubricConfidence>(entity =>
        {
            entity.HasKey(e => e.RubricConfidenceId).HasName("PK_AIRubricConfidence");
            entity.ToTable("AIRubricConfidence");
            entity.Property(e => e.FinalScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiDoctorFeedback>(entity =>
        {
            entity.HasKey(e => e.DoctorFeedbackId).HasName("PK_AIDoctorFeedback");
            entity.ToTable("AIDoctorFeedback");
            entity.Property(e => e.Action).HasMaxLength(30);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.RejectReasonStage).HasMaxLength(40);
            entity.Property(e => e.RejectReasonNote).HasMaxLength(1000);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiRolloutGate>(entity =>
        {
            entity.HasKey(e => e.RolloutGateId).HasName("PK_AIRolloutGate");
            entity.ToTable("AIRolloutGate");
            entity.Property(e => e.FlagName).HasMaxLength(100);
            entity.Property(e => e.Top5Accuracy).HasColumnType("decimal(5,4)");
            entity.Property(e => e.DoctorAcceptanceRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.BenchmarkRunUtc).HasColumnType("datetime2");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiCaseLearning>(entity =>
        {
            entity.HasKey(e => e.CaseLearningId).HasName("PK_AICaseLearning");
            entity.ToTable("AICaseLearning");
            entity.Property(e => e.WeightDelta).HasColumnType("decimal(6,3)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiMonitoringDailySnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK_AIMonitoringDailySnapshot");
            entity.ToTable("AIMonitoringDailySnapshot");
            entity.HasIndex(e => new { e.SnapshotDate, e.EngineVersion }).IsUnique();
            entity.Property(e => e.PrecisionScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.RecallScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.DoctorAcceptanceRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.HallucinationRate).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EmbeddingFreshnessHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.RubricEmbeddingCoverage).HasColumnType("decimal(5,4)");
            entity.Property(e => e.ConceptEmbeddingCoverage).HasColumnType("decimal(5,4)");
            entity.Property(e => e.TranscriptCoverage).HasColumnType("decimal(5,4)");
            entity.Property(e => e.AverageConfidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.PrimaryRubricAccuracy).HasColumnType("decimal(5,4)");
            entity.Property(e => e.F1Score).HasColumnType("decimal(5,4)");
            entity.Property(e => e.SnapshotDate).HasColumnType("date");
            entity.Property(e => e.CalculatedDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiMonitoringAuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK_AIMonitoringAuditLog");
            entity.ToTable("AIMonitoringAuditLog");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgNode>(entity =>
        {
            entity.HasKey(e => e.NodeId).HasName("PK_AIKGNode");
            entity.ToTable("AIKGNode");
            entity.HasIndex(e => new { e.NodeType, e.CanonicalKey, e.LanguageCode }).IsUnique();
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgEdge>(entity =>
        {
            entity.HasKey(e => e.EdgeId).HasName("PK_AIKGEdge");
            entity.ToTable("AIKGEdge");
            entity.Property(e => e.Weight).HasColumnType("decimal(6,3)");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgEdgeEvidence>(entity =>
        {
            entity.HasKey(e => e.EdgeEvidenceId).HasName("PK_AIKGEdgeEvidence");
            entity.ToTable("AIKGEdgeEvidence");
            entity.Property(e => e.EmbeddingScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgFeedbackMutation>(entity =>
        {
            entity.HasKey(e => e.FeedbackMutationId).HasName("PK_AIKGFeedbackMutation");
            entity.ToTable("AIKGFeedbackMutation");
            entity.Property(e => e.WeightDelta).HasColumnType("decimal(6,3)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgSessionPath>(entity =>
        {
            entity.HasKey(e => e.SessionPathId).HasName("PK_AIKGSessionPath");
            entity.ToTable("AIKGSessionPath");
            entity.Property(e => e.PathConfidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgRemedyProjection>(entity =>
        {
            entity.HasKey(e => e.RemedyProjectionId).HasName("PK_AIKGRemedyProjection");
            entity.ToTable("AIKGRemedyProjection");
            entity.HasIndex(e => new { e.SubSectionId, e.RemedyId }).IsUnique();
            entity.Property(e => e.LastSyncedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiKgFigurativeResolution>(entity =>
        {
            entity.HasKey(e => e.FigurativeResolutionId).HasName("PK_AIKGFigurativeResolution");
            entity.ToTable("AIKGFigurativeResolution");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiConceptMappingBootstrap>(entity =>
        {
            entity.HasKey(e => e.ConceptMappingBootstrapId).HasName("PK_AIConceptMappingBootstrap");
            entity.ToTable("AIConceptMappingBootstrap");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiSymptomBlock>(entity =>
        {
            entity.HasKey(e => e.SymptomBlockId).HasName("PK_AISymptomBlock");
            entity.ToTable("AISymptomBlock");
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiConceptCluster>(entity =>
        {
            entity.HasKey(e => e.ConceptClusterId).HasName("PK_AIConceptCluster");
            entity.ToTable("AIConceptCluster");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiConceptClusterMember>(entity =>
        {
            entity.HasKey(e => e.ConceptClusterMemberId).HasName("PK_AIConceptClusterMember");
            entity.ToTable("AIConceptClusterMember");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiCaseCoverageMetrics>(entity =>
        {
            entity.HasKey(e => e.CaseCoverageMetricsId).HasName("PK_AICaseCoverageMetrics");
            entity.ToTable("AICaseCoverageMetrics");
            entity.Property(e => e.TranscriptCoverage).HasColumnType("decimal(5,4)");
            entity.Property(e => e.CaseCompleteness).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiMissingSymptomCandidate>(entity =>
        {
            entity.HasKey(e => e.MissingSymptomCandidateId).HasName("PK_AIMissingSymptomCandidate");
            entity.ToTable("AIMissingSymptomCandidate");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiEmbeddingVersion>(entity =>
        {
            entity.HasKey(e => e.EmbeddingVersionId).HasName("PK_AIEmbeddingVersion");
            entity.ToTable("AIEmbeddingVersion");
            entity.Property(e => e.VersionCode).HasMaxLength(50);
            entity.Property(e => e.ModelProvider).HasMaxLength(50);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.ModelVersion).HasMaxLength(50);
            entity.Property(e => e.VectorFormat).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.DeletedDate).HasColumnType("datetime2");
            entity.HasIndex(e => e.VersionCode).IsUnique().HasDatabaseName("UQ_AIEmbeddingVersion_VersionCode");
        });

        modelBuilder.Entity<AiRubricEmbedding>(entity =>
        {
            entity.HasKey(e => e.RubricEmbeddingId).HasName("PK_AIRubricEmbedding");
            entity.ToTable("AIRubricEmbedding");
            entity.Property(e => e.SourceText).HasMaxLength(2000);
            entity.Property(e => e.TextHash).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.SourceType).HasMaxLength(30);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.DeletedDate).HasColumnType("datetime2");
            entity.HasOne<AiEmbeddingVersion>()
                .WithMany()
                .HasForeignKey(e => e.EmbeddingVersionId)
                .HasConstraintName("FK_AIRubricEmbedding_Version");
            entity.HasOne<SubSectionMaster>()
                .WithMany()
                .HasForeignKey(e => e.RubricId)
                .HasConstraintName("FK_AIRubricEmbedding_SubSection");
        });

        modelBuilder.Entity<AiConceptEmbedding>(entity =>
        {
            entity.HasKey(e => e.ConceptEmbeddingId).HasName("PK_AIConceptEmbedding");
            entity.ToTable("AIConceptEmbedding");
            entity.Property(e => e.ConceptKey).HasMaxLength(200);
            entity.Property(e => e.ConceptType).HasMaxLength(50);
            entity.Property(e => e.SourceDomain).HasMaxLength(50);
            entity.Property(e => e.SourceText).HasMaxLength(2000);
            entity.Property(e => e.TextHash).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.DeletedDate).HasColumnType("datetime2");
            entity.HasOne<AiEmbeddingVersion>()
                .WithMany()
                .HasForeignKey(e => e.EmbeddingVersionId)
                .HasConstraintName("FK_AIConceptEmbedding_Version");
        });

        modelBuilder.Entity<AiEmbeddingJob>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("PK_AIEmbeddingJob");
            entity.ToTable("AIEmbeddingJob");
            entity.Property(e => e.JobType).HasMaxLength(50);
            entity.Property(e => e.TriggerSource).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.ErrorSummary).HasMaxLength(2000);
            entity.Property(e => e.StartedAtUtc).HasColumnType("datetime2");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("datetime2");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.DeletedDate).HasColumnType("datetime2");
            entity.HasOne<AiEmbeddingVersion>()
                .WithMany()
                .HasForeignKey(e => e.EmbeddingVersionId)
                .HasConstraintName("FK_AIEmbeddingJob_Version");
        });

        modelBuilder.Entity<AiEmbeddingQueue>(entity =>
        {
            entity.HasKey(e => e.QueueId).HasName("PK_AIEmbeddingQueue");
            entity.ToTable("AIEmbeddingQueue");
            entity.Property(e => e.ItemType).HasMaxLength(30);
            entity.Property(e => e.ConceptKey).HasMaxLength(200);
            entity.Property(e => e.ConceptType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.LockedBy).HasMaxLength(100);
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.Property(e => e.NextRetryAtUtc).HasColumnType("datetime2");
            entity.Property(e => e.LockedUntilUtc).HasColumnType("datetime2");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("datetime2");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.DeletedDate).HasColumnType("datetime2");
            entity.HasOne<AiEmbeddingJob>()
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .HasConstraintName("FK_AIEmbeddingQueue_Job");
        });

        modelBuilder.Entity<AiEmbeddingAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK_AIEmbeddingAudit");
            entity.ToTable("AIEmbeddingAudit");
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.OldStatus).HasMaxLength(30);
            entity.Property(e => e.NewStatus).HasMaxLength(30);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
        });

        modelBuilder.Entity<AiEmbeddingStatistics>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PK_AIEmbeddingStatistics");
            entity.ToTable("AIEmbeddingStatistics");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.HasOne<AiEmbeddingVersion>()
                .WithMany()
                .HasForeignKey(e => e.EmbeddingVersionId)
                .HasConstraintName("FK_AIEmbeddingStatistics_Version");
            entity.HasIndex(e => new { e.EmbeddingVersionId, e.StatDate })
                .IsUnique()
                .HasDatabaseName("UQ_AIEmbeddingStatistics_Version_Date");
        });

        modelBuilder.Entity<AiEmbeddingSyncState>(entity =>
        {
            entity.HasKey(e => e.SyncStateId).HasName("PK_AIEmbeddingSyncState");
            entity.ToTable("AIEmbeddingSyncState");
            entity.Property(e => e.SyncScope).HasMaxLength(50);
            entity.Property(e => e.LastSuccessfulSyncUtc).HasColumnType("datetime2");
            entity.Property(e => e.LastScanStartedUtc).HasColumnType("datetime2");
            entity.Property(e => e.LastScanCompletedUtc).HasColumnType("datetime2");
            entity.Property(e => e.LastRunCorrelationId).HasMaxLength(64);
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.HasOne<AiEmbeddingVersion>()
                .WithMany()
                .HasForeignKey(e => e.EmbeddingVersionId)
                .HasConstraintName("FK_AIEmbeddingSyncState_Version");
        });

        modelBuilder.Entity<RepertorySource>(entity =>
        {
            entity.HasKey(e => e.RepertorySourceId).HasName("PK_RepertorySource");
            entity.ToTable("RepertorySource");
            entity.Property(e => e.SourceCode).HasMaxLength(20);
            entity.Property(e => e.SourceName).HasMaxLength(100);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasIndex(e => e.SourceCode).IsUnique().HasDatabaseName("UQ_RepertorySource_SourceCode");
        });

        modelBuilder.Entity<RubricRepertoryMap>(entity =>
        {
            entity.HasKey(e => e.RubricRepertoryMapId).HasName("PK_RubricRepertoryMap");
            entity.ToTable("RubricRepertoryMap");
            entity.Property(e => e.SourceRubricKey).HasMaxLength(2000);
            entity.Property(e => e.SourceRubricPath).HasMaxLength(1000);
            entity.Property(e => e.MappingConfidence).HasColumnType("decimal(5,4)");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasIndex(e => new { e.SubSectionId, e.RepertorySourceId })
                .IsUnique()
                .HasDatabaseName("UQ_RubricRepertoryMap_SubSectionSource");
            entity.HasOne(e => e.RepertorySource)
                .WithMany()
                .HasForeignKey(e => e.RepertorySourceId)
                .HasConstraintName("FK_RubricRepertoryMap_Source");
            entity.HasOne<SubSectionMaster>()
                .WithMany()
                .HasForeignKey(e => e.SubSectionId)
                .HasConstraintName("FK_RubricRepertoryMap_SubSection");
        });

        modelBuilder.Entity<AudioCaseDoctorActionLog>(entity =>
        {
            entity.HasKey(e => e.DoctorActionLogId).HasName("PK_AudioCaseDoctorActionLog");
            entity.ToTable("AudioCaseDoctorActionLog");
            entity.Property(e => e.ActionType).HasMaxLength(80);
            entity.Property(e => e.TargetType).HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseDoctorActionLog_Session");
        });

        modelBuilder.Entity<AudioCaseRetentionLog>(entity =>
        {
            entity.HasKey(e => e.RetentionLogId).HasName("PK_AudioCaseRetentionLog");
            entity.ToTable("AudioCaseRetentionLog");
            entity.Property(e => e.ActionType).HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.PerformedBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.HasOne<AudioCaseSession>()
                .WithMany()
                .HasForeignKey(e => e.AudioCaseSessionId)
                .HasConstraintName("FK_AudioCaseRetentionLog_Session");
        });

        modelBuilder.Entity<DrugGroupMaster>(entity =>
        {
            entity.HasKey(e => e.DrugGroupId);

            entity.ToTable("DrugGroupMaster");

            entity.HasOne(d => d.DrugSystem).WithMany(p => p.DrugGroupMasters)
                .HasForeignKey(d => d.DrugSystemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DrugGroupMaster_DrugSystemMaster");
        });

        modelBuilder.Entity<DrugSystemMaster>(entity =>
        {
            entity.HasKey(e => e.DrugSystemId);

            entity.ToTable("DrugSystemMaster");
        });

        modelBuilder.Entity<EmergencieDetail>(entity =>
        {
            entity.HasKey(e => e.EmergencieId);

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.EmergencieDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .HasConstraintName("FK_EmergencieDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<EmergencieRubricDetail>(entity =>
        {
            entity.HasKey(e => e.EmergencieRubricId);

            entity.HasOne(d => d.Emergencie).WithMany(p => p.EmergencieRubricDetails)
                .HasForeignKey(d => d.EmergencieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmergencieRubricDetails_EmergencieDetails");
        });

        modelBuilder.Entity<EnquiryDetail>(entity =>
        {
            entity.HasKey(e => e.EnquiryId);

            entity.Property(e => e.EmailId).HasMaxLength(100);
            entity.Property(e => e.EnquiryDate).HasColumnType("datetime");
            entity.Property(e => e.EnquiryName).HasMaxLength(100);
            entity.Property(e => e.MobileNo).HasMaxLength(15);
        });

        modelBuilder.Entity<FirmDetail>(entity =>
        {
            entity.HasKey(e => e.FirmId);

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DatabaseBackupPath).HasColumnName("DatabaseBackupPAth");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.FirmBranchName).HasMaxLength(250);
            entity.Property(e => e.FirmBranchNameMarathi).HasMaxLength(250);
            entity.Property(e => e.FirmConnectionPath).HasMaxLength(250);
            entity.Property(e => e.FirmEmailIid).HasMaxLength(50);
            entity.Property(e => e.FirmFaxNumber).HasMaxLength(15);
            entity.Property(e => e.FirmLogo).HasMaxLength(450);
            entity.Property(e => e.FirmName).HasMaxLength(250);
            entity.Property(e => e.FirmNameMarathi).HasMaxLength(250);
            entity.Property(e => e.FirmOfficeAddress).HasMaxLength(250);
            entity.Property(e => e.FirmOfficeAddressMarathi).HasMaxLength(250);
            entity.Property(e => e.FirmPhoneNumber).HasMaxLength(50);
            entity.Property(e => e.FirmRegDate).HasColumnType("datetime");
            entity.Property(e => e.FirmRegNumber).HasMaxLength(250);
            entity.Property(e => e.LanguageIds).HasMaxLength(50);
            entity.Property(e => e.MailPassword).HasMaxLength(30);
            entity.Property(e => e.ModuleIds).HasMaxLength(50);
        });

        modelBuilder.Entity<GenderMaster>(entity =>
        {
            entity.HasKey(e => e.GenderId);

            entity.ToTable("GenderMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.GenderName).HasMaxLength(10);
        });

        modelBuilder.Entity<HumanSystemMaster>(entity =>
        {
            entity.HasKey(e => e.HumanSystemId);

            entity.ToTable("HumanSystemMaster");

            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<IntensityMaster>(entity =>
        {
            entity.HasKey(e => e.IntensityId);

            entity.ToTable("IntensityMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<LabTestMaster>(entity =>
        {
            entity.HasKey(e => e.TestId);

            entity.ToTable("LabTestMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(500);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(500);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.TestName).HasMaxLength(1000);
        });

        modelBuilder.Entity<LanguageMaster>(entity =>
        {
            entity.HasKey(e => e.LanguageId);

            entity.ToTable("LanguageMaster");

            entity.Property(e => e.LanguageId).HasColumnName("languageId");
            entity.Property(e => e.LanguageName).HasColumnName("languageName");
        });

        modelBuilder.Entity<LanguageVersion>(entity =>
        {
            entity.HasKey(e => e.LanguageId);

            entity.ToTable("LanguageVersion");

            entity.Property(e => e.LanguageLogo).HasMaxLength(250);
            entity.Property(e => e.LanguageName).HasMaxLength(50);
        });

        modelBuilder.Entity<LocationExtentionDetail>(entity =>
        {
            entity.HasKey(e => e.LocationExtentionDetailsId);

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.LocationExtentionDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LocationExtentionDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<LocationExtentionRubricDetail>(entity =>
        {
            entity.HasKey(e => e.LocationExtentionRubricDetailsId);

            entity.HasOne(d => d.LocationExtentionDetails).WithMany(p => p.LocationExtentionRubricDetails)
                .HasForeignKey(d => d.LocationExtentionDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LocationExtentionRubricDetails_LocationExtentionDetails");
        });

        modelBuilder.Entity<MateriaMedicaDetail>(entity =>
        {
            entity.HasKey(e => e.MatriaMedicaDetailId);

            entity.ToTable("MateriaMedicaDetail");

            entity.Property(e => e.MateriaMedicaDetail1).HasColumnName("MateriaMedicaDetail");

            entity.HasOne(d => d.MateriaMedica).WithMany(p => p.MateriaMedicaDetails)
                .HasForeignKey(d => d.MateriaMedicaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MateriaMedicaDetail_MateriaMedicaMaster");
        });

        modelBuilder.Entity<MateriaMedicaHeadMaster>(entity =>
        {
            entity.HasKey(e => e.MateriaMedicaHeadId);

            entity.ToTable("MateriaMedicaHeadMaster");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DifferentialMm).HasColumnName("DifferentialMM");
            entity.Property(e => e.MateriaMedicaHeadName).HasMaxLength(1000);

            entity.HasOne(d => d.Author).WithMany(p => p.MateriaMedicaHeadMasters)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK_MateriaMedicaHeadMaster_AuthorMaster");
        });

        modelBuilder.Entity<MateriaMedicaMaster>(entity =>
        {
            entity.HasKey(e => e.MateriaMedicaId);

            entity.ToTable("MateriaMedicaMaster");

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Dose).HasMaxLength(500);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");

            entity.HasOne(d => d.Author).WithMany(p => p.MateriaMedicaMasters)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK_MateriaMedicaMaster_AuthorMaster");

            entity.HasOne(d => d.MateriaMedicaHead).WithMany(p => p.MateriaMedicaMasters)
                .HasForeignKey(d => d.MateriaMedicaHeadId)
                .HasConstraintName("FK_MateriaMedicaMaster_MateriaMedicaHeadMaster");

            entity.HasOne(d => d.Remedy).WithMany(p => p.MateriaMedicaMasters)
                .HasForeignKey(d => d.RemedyId)
                .HasConstraintName("FK_MateriaMedicaMaster_RemedyMaster");
        });

        modelBuilder.Entity<MedicalAstrologyMaster>(entity =>
        {
            entity.HasKey(e => e.AstrologyId);

            entity.ToTable("MedicalAstrologyMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DeleteStatus).HasDefaultValue(false);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");

            entity.HasOne(d => d.Disease).WithMany(p => p.MedicalAstrologyMasters)
                .HasForeignKey(d => d.DiseaseId)
                .HasConstraintName("FK_MedicalAstrologyMaster_DiseaseMaster");
        });

        modelBuilder.Entity<MenuMaster>(entity =>
        {
            entity.HasKey(e => e.MenuId);

            entity.ToTable("MenuMaster");

            entity.Property(e => e.ActionName).HasMaxLength(50);
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.ControllerName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.IsLeaf).HasDefaultValue(true);
            entity.Property(e => e.MenuIcon).HasMaxLength(250);
            entity.Property(e => e.MenuName).HasMaxLength(50);
            entity.Property(e => e.MenuNameMarathi).HasMaxLength(50);
            entity.Property(e => e.MenuType).HasMaxLength(50);
            entity.Property(e => e.MenuUrl).HasMaxLength(250);
            entity.Property(e => e.ShowInMainMenu).HasDefaultValue(true);

            entity.HasOne(d => d.Module).WithMany(p => p.MenuMasters)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MenuMaster_ModuleMaster");
        });

        modelBuilder.Entity<ModalitiesDetail>(entity =>
        {
            entity.HasKey(e => e.ModalitiesDetailsId);

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.ModalitiesDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ModalitiesDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<ModalitiesRubricDetail>(entity =>
        {
            entity.HasKey(e => e.ModalitiesRubricDetailsId);

            entity.HasOne(d => d.ModalitiesDetails).WithMany(p => p.ModalitiesRubricDetails)
                .HasForeignKey(d => d.ModalitiesDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ModalitiesRubricDetails_ModalitiesDetails");
        });

        modelBuilder.Entity<ModuleMaster>(entity =>
        {
            entity.HasKey(e => e.ModuleId);

            entity.ToTable("ModuleMaster");

            entity.Property(e => e.ActionName)
                .HasMaxLength(150)
                .HasDefaultValue("Browse");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.ControllerName)
                .HasMaxLength(50)
                .HasDefaultValue("Layout");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ModuleAreaName).HasMaxLength(20);
            entity.Property(e => e.ModuleIcon).HasMaxLength(250);
            entity.Property(e => e.ModuleMarathiName).HasMaxLength(50);
            entity.Property(e => e.ModuleName).HasMaxLength(50);
            entity.Property(e => e.ModuleUrl).HasMaxLength(250);
        });

        modelBuilder.Entity<Monogram>(entity =>
        {
            entity.ToTable("Monogram");

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.Monogram1)
                .HasMaxLength(1000)
                .HasColumnName("Monogram");
        });

        modelBuilder.Entity<MonogramDetail>(entity =>
        {
            entity.HasOne(d => d.Monogram).WithMany(p => p.MonogramDetails)
                .HasForeignKey(d => d.MonogramId)
                .HasConstraintName("FK_MonogramDetails_Monogram");
        });

        modelBuilder.Entity<NewsCategory>(entity =>
        {
            entity.ToTable("NewsCategory");

            entity.Property(e => e.NewsCategory1)
                .HasMaxLength(1000)
                .HasColumnName("NewsCategory");
        });

        modelBuilder.Entity<NewsDetail>(entity =>
        {
            entity.HasKey(e => e.NewsId);

            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.NewsDate).HasColumnType("datetime");

            entity.HasOne(d => d.NewsCategory).WithMany(p => p.NewsDetails)
                .HasForeignKey(d => d.NewsCategoryId)
                .HasConstraintName("FK_NewsDetails_NewsCategory");
        });

        modelBuilder.Entity<ObservationsDetail>(entity =>
        {
            entity.HasKey(e => e.ObservationsDetailsId);

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.ObservationsDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ObservationsDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<ObservationsRubricDetail>(entity =>
        {
            entity.HasKey(e => e.ObservationsRubricDetailsId);

            entity.HasOne(d => d.ObservationsDetails).WithMany(p => p.ObservationsRubricDetails)
                .HasForeignKey(d => d.ObservationsDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ObservationsRubricDetails_ObservationsDetails");
        });

        modelBuilder.Entity<OnsetDurationProgressDetail>(entity =>
        {
            entity.HasKey(e => e.OnsetDetailId).HasName("PK_Onset_Duration_ProgressDetails");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.OnsetDurationProgressDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Onset_Duration_ProgressDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<OnsetDurationProgressRubricDetail>(entity =>
        {
            entity.HasKey(e => e.OnsetRubricId).HasName("PK_Onset_Duration_ProgressRubricDetails");

            entity.HasOne(d => d.OnsetDetail).WithMany(p => p.OnsetDurationProgressRubricDetails)
                .HasForeignKey(d => d.OnsetDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Onset_Duration_ProgressRubricDetails_Onset_Duration_ProgressDetails");
        });

        modelBuilder.Entity<OtherSideEffectMaster>(entity =>
        {
            entity.HasKey(e => e.OtherSideEffectId);

            entity.ToTable("OtherSideEffectMaster");

            entity.HasOne(d => d.AllopathicDrug).WithMany(p => p.OtherSideEffectMasters)
                .HasForeignKey(d => d.AllopathicDrugId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OtherSideEffectMaster_AllopathicDrugMaster");
        });

        modelBuilder.Entity<PackageEntryDetail>(entity =>
        {
            entity.HasKey(e => e.PackageDetailId);

            entity.Property(e => e.ActivationDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.OrderId).HasMaxLength(512);
            entity.Property(e => e.PaymentId).HasMaxLength(512);
            entity.Property(e => e.TransactionId).HasMaxLength(512);

            entity.HasOne(d => d.Doctor).WithMany(p => p.PackageEntryDetails)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_PackageEntryDetails_Doctor");

            entity.HasOne(d => d.Package).WithMany(p => p.PackageEntryDetails)
                .HasForeignKey(d => d.PackageId)
                .HasConstraintName("FK_PackageEntryDetails_PackageMaster");
        });

        modelBuilder.Entity<PackageMaster>(entity =>
        {
            entity.HasKey(e => e.PackageId);

            entity.ToTable("PackageMaster");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.PackageName).HasMaxLength(100);
        });

        modelBuilder.Entity<PackageTopupMaster>(entity =>
        {
            entity.HasKey(e => e.PackageTopupId);

            entity.ToTable("PackageTopupMaster");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.PackageTopupName).HasMaxLength(100);
        });

        modelBuilder.Entity<PartLocationMaster>(entity =>
        {
            entity.HasKey(e => e.PartLocationId);

            entity.ToTable("PartLocationMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.PartLocationName).HasMaxLength(200);
        });

        modelBuilder.Entity<Pathology>(entity =>
        {
            entity.ToTable("Pathology");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PathologyName).HasMaxLength(1000);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK_PatientMaster");

            entity.ToTable("Patient");

            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.MobileNo).HasMaxLength(15);
            entity.Property(e => e.PatientName).HasMaxLength(200);
            entity.Property(e => e.PhoneNo).HasMaxLength(20);
            entity.Property(e => e.IsWhatsAppOptIn)
                .HasColumnName("IsWhatsAppOptIn")
                .HasDefaultValue(false);
            entity.Property(e => e.WhatsAppOptInDate)
                .HasColumnName("WhatsAppOptInDate")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Country).WithMany(p => p.Patients)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("FK_PatientMaster_CountryMaster");

            entity.HasOne(d => d.State).WithMany(p => p.Patients)
                .HasForeignKey(d => d.StateId)
                .HasConstraintName("FK_PatientMaster_StateMaster");
        });

        modelBuilder.Entity<PatientAppointment>(entity =>
        {
            entity.HasKey(e => e.PatientAppId);

            entity.ToTable("PatientAppointment");

            entity.Property(e => e.AppointmentDate).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Doctor).WithMany(p => p.PatientAppointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientAppointment_Doctor");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientAppointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientAppointment_PatientAppointment");
        });

        modelBuilder.Entity<PatientLabEntry>(entity =>
        {
            entity.HasKey(e => e.PatientLabId);

            entity.ToTable("PatientLabEntry");

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.LabDate).HasColumnType("datetime");
            entity.Property(e => e.ParameterName).HasMaxLength(500);
            entity.Property(e => e.ParameterValue).HasMaxLength(500);

            entity.HasOne(d => d.PatientLabTest).WithMany(p => p.PatientLabEntries)
                .HasForeignKey(d => d.PatientLabTestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientLabEntry_PatientLabTestMaster");
        });

        modelBuilder.Entity<PatientLabOrder>(entity =>
        {
            entity.HasKey(e => e.PatientOrderedTestId);

            entity.ToTable("PatientLabOrder");

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.LabName).HasMaxLength(500);
            entity.Property(e => e.OrderDate).HasColumnType("datetime");

            entity.HasOne(d => d.PatientLabTest).WithMany(p => p.PatientLabOrders)
                .HasForeignKey(d => d.PatientLabTestId)
                .HasConstraintName("FK_PatientLabOrder_PatientLabTestMaster");
        });

        modelBuilder.Entity<PatientLabTestMaster>(entity =>
        {
            entity.HasKey(e => e.PatientLabTestId);

            entity.ToTable("PatientLabTestMaster");

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.LabTestName).HasMaxLength(1000);
        });

        modelBuilder.Entity<PatternRubricDetail>(entity =>
        {
            entity.HasKey(e => e.PatternRubricDetailsId);

            entity.HasOne(d => d.PatternDetails).WithMany(p => p.PatternRubricDetails)
                .HasForeignKey(d => d.PatternDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatternRubricDetails_PatternRubricDetails");
        });

        modelBuilder.Entity<PatternsDetail>(entity =>
        {
            entity.HasKey(e => e.PatternDetailsId);

            entity.ToTable("PatternsDetail");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.PatternsDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatternsDetail_DiagnosisMaster");
        });

        modelBuilder.Entity<PrescriptionRemedyDetail>(entity =>
        {
            entity.HasKey(e => e.PrescriptionRemedyId);

            entity.ToTable("PrescriptionRemedyDetail");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Dose).HasMaxLength(250);

            entity.HasOne(d => d.Appointment).WithMany(p => p.PrescriptionRemedyDetails)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrescriptionRemedyDetail_PatientAppointment");
        });

        modelBuilder.Entity<PrescriptionRubricDetail>(entity =>
        {
            entity.HasKey(e => e.PrescriptionRubricId);

            entity.ToTable("PrescriptionRubricDetail");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Appointment).WithMany(p => p.PrescriptionRubricDetails)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrescriptionRubricDetail_PatientAppointment");
        });

        modelBuilder.Entity<PsChangeDate>(entity =>
        {
            entity.HasKey(e => e.ChangeDateId);

            entity.ToTable("PsChangeDate");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.CloseMonth).HasColumnType("datetime");
            entity.Property(e => e.CloseYear).HasColumnType("datetime");
            entity.Property(e => e.CurrDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<QualificationMaster>(entity =>
        {
            entity.HasKey(e => e.QualificationId);

            entity.ToTable("QualificationMaster");

            entity.Property(e => e.QualificationId).HasColumnName("QualificationID");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DegreeLevel).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.QualificationAlias).HasMaxLength(20);
            entity.Property(e => e.QualificationName).HasMaxLength(200);
        });

        modelBuilder.Entity<QuestionGroupMaster>(entity =>
        {
            entity.HasKey(e => e.QuestionGroupId);

            entity.ToTable("QuestionGroupMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.QuestionGroupName).HasMaxLength(500);

            entity.HasOne(d => d.Section).WithMany(p => p.QuestionGroupMasters)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK_QuestionGroupMaster_SectionMaster");
        });

        modelBuilder.Entity<QuestionSectionMaster>(entity =>
        {
            entity.HasKey(e => e.QuestionSectionId);

            entity.ToTable("QuestionSectionMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Desciption).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.QuestionSectionName).HasMaxLength(200);
        });

        modelBuilder.Entity<QuestionSubgroup>(entity =>
        {
            entity.ToTable("QuestionSubgroup");

            entity.Property(e => e.Description).HasMaxLength(1500);
            entity.Property(e => e.QuestionSubgroup1)
                .HasMaxLength(1000)
                .HasColumnName("QuestionSubgroup");
        });

        modelBuilder.Entity<ReferenceRubricDetail>(entity =>
        {
            entity.HasKey(e => e.ReferenceRubricId);

            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");

            entity.HasOne(d => d.RefSubSection).WithMany(p => p.ReferenceRubricDetailRefSubSections)
                .HasForeignKey(d => d.RefSubSectionId)
                .HasConstraintName("FK_ReferenceRubricDetails_SubSectionMaster1");

            entity.HasOne(d => d.SubSection).WithMany(p => p.ReferenceRubricDetailSubSections)
                .HasForeignKey(d => d.SubSectionId)
                .HasConstraintName("FK_ReferenceRubricDetails_SubSectionMaster");
        });

        modelBuilder.Entity<RemedyGradeMaster>(entity =>
        {
            entity.HasKey(e => e.GradeId);

            entity.ToTable("RemedyGradeMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(50);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.FontColor).HasMaxLength(50);
            entity.Property(e => e.FontName).HasMaxLength(100);
            entity.Property(e => e.FontStyle).HasMaxLength(50);
        });

        modelBuilder.Entity<RemedyMaster>(entity =>
        {
            entity.HasKey(e => e.RemedyId).HasName("PK_Remedy");

            entity.ToTable("RemedyMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.RemedyAlias).HasMaxLength(100);
            entity.Property(e => e.RemedyName).HasMaxLength(500);

            entity.HasOne(d => d.Thermal).WithMany(p => p.RemedyMasters)
                .HasForeignKey(d => d.ThermalId)
                .HasConstraintName("FK_RemedyMaster_ThermalMaster");
        });

        modelBuilder.Entity<RemedyRubricAuthorDetail>(entity =>
        {
            entity.HasKey(e => e.RemedyRubricAuthorId);

            // Table has SQL triggers; EF must not use OUTPUT clause on SaveChanges.
            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.Property(e => e.DeletedStatus).HasDefaultValue(false);

            entity.HasOne(d => d.Author).WithMany(p => p.RemedyRubricAuthorDetails)
                .HasForeignKey(d => d.AuthorId)
                .HasConstraintName("FK_RemedyRubricAuthorDetails_AuthorMaster");

            entity.HasOne(d => d.RubricRemedy).WithMany(p => p.RemedyRubricAuthorDetails)
                .HasForeignKey(d => d.RubricRemedyId)
                .HasConstraintName("FK_RemedyRubricAuthorDetails_RubricRemedyDetails");
        });

        modelBuilder.Entity<ReportSetting>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK_ReportSetting");

            entity.Property(e => e.Applicablefor).HasMaxLength(50);
            entity.Property(e => e.BranchAddressFontSize).HasMaxLength(50);
            entity.Property(e => e.BranchFontSize).HasMaxLength(50);
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.FilterCriteria).HasMaxLength(450);
            entity.Property(e => e.MethodName)
                .HasMaxLength(450)
                .IsFixedLength();
            entity.Property(e => e.MultipleIssue).HasDefaultValue(true);
            entity.Property(e => e.ReportFont).HasMaxLength(50);
            entity.Property(e => e.ReportFontSize).HasMaxLength(50);
            entity.Property(e => e.ReportName).HasMaxLength(50);
            entity.Property(e => e.TrustFontSize).HasMaxLength(50);

            entity.HasOne(d => d.Menu).WithMany(p => p.ReportSettings)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportSetting_MenuMaster");
        });

        modelBuilder.Entity<RoleDetail>(entity =>
        {
            entity.HasKey(e => e.RecordId);

            entity.Property(e => e.IsAdd).HasDefaultValue(true);
            entity.Property(e => e.IsDelete).HasDefaultValue(true);
            entity.Property(e => e.IsModify).HasDefaultValue(true);
            entity.Property(e => e.IsView).HasDefaultValue(true);

            entity.HasOne(d => d.Menu).WithMany(p => p.RoleDetails)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoleDetails_MenuMaster");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleDetails)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoleDetails_RoleMaster");
        });

        modelBuilder.Entity<RoleMaster>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.ToTable("RoleMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.FirmIds).HasMaxLength(250);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<RubricRemedyDetail>(entity =>
        {
            entity.HasKey(e => e.RubricRemedyId);

            // Table has SQL triggers; EF must not use OUTPUT clause on SaveChanges.
            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.Property(e => e.EnteredDate).HasColumnType("datetime");

            entity.HasOne(d => d.Grade).WithMany(p => p.RubricRemedyDetails)
                .HasForeignKey(d => d.GradeId)
                .HasConstraintName("FK_RubricRemedyDetails_RemedyGradeMaster");

            entity.HasOne(d => d.Remedy).WithMany(p => p.RubricRemedyDetails)
                .HasForeignKey(d => d.RemedyId)
                .HasConstraintName("FK_RubricRemedyDetails_RemedyMaster");

            entity.HasOne(d => d.SubSection).WithMany(p => p.RubricRemedyDetails)
                .HasForeignKey(d => d.SubSectionId)
                .HasConstraintName("FK_RubricRemedyDetails_SubSectionMaster");
        });

        modelBuilder.Entity<SampleTable>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("SampleTable");
        });

        modelBuilder.Entity<SearchPageSetting>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK_SearchPageSetting");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DataKeyName).HasMaxLength(50);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ExceptTableNames).HasMaxLength(250);
            entity.Property(e => e.FilterCriteria).HasMaxLength(250);
            entity.Property(e => e.MethodName).HasMaxLength(50);
            entity.Property(e => e.TableName).HasMaxLength(50);

            entity.HasOne(d => d.Menu).WithMany(p => p.SearchPageSettings)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SearchPageSetting_MenuMaster");
        });

        modelBuilder.Entity<SectionGroupMaster>(entity =>
        {
            entity.HasKey(e => e.SectionGroupId);

            entity.ToTable("SectionGroupMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(50);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.SectionGroupName).HasMaxLength(100);
        });

        modelBuilder.Entity<SectionMaster>(entity =>
        {
            entity.HasKey(e => e.SectionId);

            // Table has SQL triggers; EF must not use OUTPUT clause on SaveChanges.
            entity.ToTable("SectionMaster", tb => tb.UseSqlOutputClause(false));

            entity.Property(e => e.SectionId).HasColumnName("SectionID");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.SectionAlias).HasMaxLength(50);
            entity.Property(e => e.SectionName).HasMaxLength(200);

            entity.HasOne(d => d.BodyPartSection).WithMany(p => p.SectionMasters)
                .HasForeignKey(d => d.BodyPartSectionId)
                .HasConstraintName("FK_SectionMaster_BodyPartSectionMaster");
        });

        modelBuilder.Entity<SensationDetail>(entity =>
        {
            entity.HasKey(e => e.SensationDetailsId);

            entity.Property(e => e.DiagnosisId).HasColumnName("DiagnosisID");

            entity.HasOne(d => d.Diagnosis).WithMany(p => p.SensationDetails)
                .HasForeignKey(d => d.DiagnosisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SensationDetails_DiagnosisMaster");
        });

        modelBuilder.Entity<SensationRubricDetail>(entity =>
        {
            entity.HasKey(e => e.SensationRubricDetailsId);

            entity.HasOne(d => d.SensationDetails).WithMany(p => p.SensationRubricDetails)
                .HasForeignKey(d => d.SensationDetailsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SensationRubricDetails_SensationDetails");
        });

        modelBuilder.Entity<SeriousSideEffectMaster>(entity =>
        {
            entity.HasKey(e => e.SeriousSideEffectId);

            entity.ToTable("SeriousSideEffectMaster");

            entity.HasOne(d => d.AllopathicDrug).WithMany(p => p.SeriousSideEffectMasters)
                .HasForeignKey(d => d.AllopathicDrugId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeriousSideEffectMaster_AllopathicDrugMaster");
        });

        modelBuilder.Entity<StateMaster>(entity =>
        {
            entity.HasKey(e => e.StateId);

            entity.ToTable("StateMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.StateName).HasMaxLength(100);

            entity.HasOne(d => d.Country).WithMany(p => p.StateMasters)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("FK_StateMaster_CountryMaster");
        });

        modelBuilder.Entity<SubSectionLanguageDetail>(entity =>
        {
            entity.HasKey(e => e.SubSectionLanguageId);

            entity.HasOne(d => d.Language).WithMany(p => p.SubSectionLanguageDetails)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubSectionLanguageDetails_LanguageMaster");

            entity.HasOne(d => d.SubSection).WithMany(p => p.SubSectionLanguageDetails)
                .HasForeignKey(d => d.SubSectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubSectionLanguageDetails_SubSectionMaster");
        });

        modelBuilder.Entity<SubSectionMaster>(entity =>
        {
            entity.HasKey(e => e.SubSectionId);

            // Table has SQL triggers; EF must not use OUTPUT clause on SaveChanges.
            entity.ToTable("SubSectionMaster", tb => tb.UseSqlOutputClause(false));

            entity.Property(e => e.SubSectionId).HasColumnName("SubSectionID");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ParentSubSectionId).HasColumnName("ParentSubSectionID");
            entity.Property(e => e.SectionId).HasColumnName("SectionID");

            entity.HasOne(d => d.Section).WithMany(p => p.SubSectionMasters)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK_SubSectionMaster_SectionMaster");
        });

        modelBuilder.Entity<ThermalMaster>(entity =>
        {
            entity.HasKey(e => e.ThermalId);

            entity.ToTable("ThermalMaster");

            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.ThermalName).HasMaxLength(50);
        });

        modelBuilder.Entity<ThreeDBodyPartMeshKeyMaster>(entity =>
        {
            entity.HasKey(e => e.ThreeDBodyPartMeshKeyId);

            entity.ToTable("ThreeDBodyPartMeshKeyMaster");

            entity.Property(e => e.ThreeDBodyPartMeshKeyId).HasColumnName("ThreeD_BodyPart_MeshKeyID");
            entity.Property(e => e.ThreeDBodyPartMeshKeyName)
                .HasColumnName("ThreeD_BodyPart_MeshKey_Name")
                .HasMaxLength(200);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<ThreeDBodyPartSectionMaster>(entity =>
        {
            entity.HasKey(e => e.ThreeDBodyPartSectionMasterId);

            entity.ToTable("ThreeDBodyPartSectionMaster");

            entity.Property(e => e.ThreeDBodyPartSectionMasterId).HasColumnName("ThreeDBodyPartSectionMasterID");
            entity.Property(e => e.ThreeDBodyPartMeshKeyId).HasColumnName("ThreeD_BodyPart_MeshKeyID");
            entity.Property(e => e.ThreeDBodyPartSectionId).HasColumnName("ThreeDBodyPartSectionID");
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");

            entity.HasOne(d => d.ThreeDBodyPartMeshKey).WithMany(p => p.ThreeDBodyPartSectionMasters)
                .HasForeignKey(d => d.ThreeDBodyPartMeshKeyId)
                .HasConstraintName("FK_ThreeDBodyPartSectionMaster_ThreeDBodyPartMeshKeyMaster");

            entity.HasOne(d => d.Section).WithMany()
                .HasForeignKey(d => d.ThreeDBodyPartSectionId)
                .HasPrincipalKey(s => s.SectionId)
                .HasConstraintName("FK_ThreeDBodyPartSectionMaster_SectionMaster");
        });

        modelBuilder.Entity<ThreeDBodyPartSectionHotspot>(entity =>
        {
            entity.HasKey(e => e.SectionHotspotId);

            entity.ToTable("ThreeDBodyPartSectionHotspot");

            entity.Property(e => e.SectionHotspotId).HasColumnName("SectionHotspotID");
            entity.Property(e => e.SectionId).HasColumnName("SectionID");
            entity.Property(e => e.HotspotName).HasMaxLength(200);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TypeofSymptomsGroupMaster>(entity =>
        {
            entity.HasKey(e => e.TypeofSymptomsGroupId);

            entity.ToTable("TypeofSymptomsGroupMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.TypeofSymptomsGroupName).HasMaxLength(200);
        });

        modelBuilder.Entity<TypeofSymptomsMaster>(entity =>
        {
            entity.HasKey(e => e.TypeofSymptomsId);

            entity.ToTable("TypeofSymptomsMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.TypeofSymptomsName).HasMaxLength(200);

            entity.HasOne(d => d.SectionGroup).WithMany(p => p.TypeofSymptomsMasters)
                .HasForeignKey(d => d.SectionGroupId)
                .HasConstraintName("FK_TypeofSymptomsMaster_SectionGroupMaster");

            entity.HasOne(d => d.Section).WithMany(p => p.TypeofSymptomsMasters)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK_TypeofSymptomsMaster_SectionMaster");

            entity.HasOne(d => d.TypeofSymptomsGroup).WithMany(p => p.TypeofSymptomsMasters)
                .HasForeignKey(d => d.TypeofSymptomsGroupId)
                .HasConstraintName("FK_TypeofSymptomsMaster_TypeofSymptomsGroupMaster");
        });

        modelBuilder.Entity<UserDetail>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK_UserDetail");

            entity.Property(e => e.IsAdd).HasDefaultValue(true);
            entity.Property(e => e.IsDelete).HasDefaultValue(true);
            entity.Property(e => e.IsModify).HasDefaultValue(true);
            entity.Property(e => e.IsView).HasDefaultValue(true);

            entity.HasOne(d => d.Firm).WithMany(p => p.UserDetails)
                .HasForeignKey(d => d.FirmId)
                .HasConstraintName("FK_UserDetail_FirmDetails");

            entity.HasOne(d => d.Menu).WithMany(p => p.UserDetails)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserDetail_MenuMaster");
        });

        modelBuilder.Entity<UserLoginStatus>(entity =>
        {
            entity.HasKey(e => e.LoginId);

            entity.ToTable("UserLoginStatus");

            entity.Property(e => e.LoginId).ValueGeneratedNever();
            entity.Property(e => e.InTime).HasColumnType("datetime");
            entity.Property(e => e.LogDate).HasColumnType("datetime");
            entity.Property(e => e.MachineNo).HasMaxLength(50);
            entity.Property(e => e.OutTime).HasColumnType("datetime");
            entity.Property(e => e.Satus).HasDefaultValue(true);
        });

        modelBuilder.Entity<WhatsAppMessageLog>(entity =>
        {
            entity.HasKey(e => e.WhatsAppMessageLogId).HasName("PK_WhatsAppMessageLog");

            entity.ToTable("WhatsAppMessageLog");

            entity.HasIndex(e => e.DoctorId, "IX_WhatsAppMessageLog_DoctorID");
            entity.HasIndex(e => e.PatientId, "IX_WhatsAppMessageLog_PatientID");
            entity.HasIndex(e => e.CampaignId, "IX_WhatsAppMessageLog_CampaignID");
            entity.HasIndex(e => e.MobileNumber, "IX_WhatsAppMessageLog_MobileNumber");
            entity.HasIndex(e => e.CreatedDate, "IX_WhatsAppMessageLog_CreatedDate");

            entity.Property(e => e.WhatsAppMessageLogId).HasColumnName("WhatsAppMessageLogID");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.PatientId).HasColumnName("PatientID");
            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.TemplateId).HasColumnName("TemplateID");
            entity.Property(e => e.LanguageId).HasColumnName("LanguageId");
            entity.Property(e => e.PatientName).HasMaxLength(250);
            entity.Property(e => e.MobileNumber).HasMaxLength(50);
            entity.Property(e => e.MessageCategory).HasMaxLength(50);
            entity.Property(e => e.MetaMessageId).HasMaxLength(500);
            entity.Property(e => e.MediaId).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.IsBulk).HasDefaultValue(false);
            entity.Property(e => e.SendStatus).HasDefaultValue(false);
            entity.Property(e => e.DeleteStatus).HasDefaultValue(false);

            entity.HasOne(d => d.Language).WithMany()
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WhatsAppMessageLog_LanguageMaster");
        });

        modelBuilder.Entity<WhatsAppTemplateMaster>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK_WhatsAppTemplateMaster");

            entity.ToTable("WhatsAppTemplateMaster");

            entity.Property(e => e.TemplateName).HasMaxLength(200);
            entity.Property(e => e.TemplateCategory).HasMaxLength(50);
            entity.Property(e => e.MetaTemplateName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DeleteStatus).HasDefaultValue(false);

            entity.HasOne(d => d.Language).WithMany(p => p.WhatsAppTemplateMasters)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WhatsAppTemplateMaster_LanguageMaster");
        });

        modelBuilder.Entity<WhatsAppCampaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId).HasName("PK_WhatsAppCampaign");

            entity.ToTable("WhatsAppCampaign");

            entity.HasIndex(e => e.DoctorId, "IX_WhatsAppCampaign_DoctorID");
            entity.HasIndex(e => e.CampaignCategory, "IX_WhatsAppCampaign_Category");

            entity.Property(e => e.CampaignId).HasColumnName("CampaignID");
            entity.Property(e => e.CampaignName).HasMaxLength(200);
            entity.Property(e => e.CampaignCategory).HasMaxLength(50);
            entity.Property(e => e.DoctorId).HasColumnName("DoctorID");
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.IsBulk).HasDefaultValue(false);
            entity.Property(e => e.DeleteStatus).HasDefaultValue(false);
        });

        modelBuilder.Entity<UserMaster>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("UserMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.EmailId).HasMaxLength(50);
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(250);
            entity.Property(e => e.LastName).HasMaxLength(250);
            entity.Property(e => e.MobileNo).HasMaxLength(20);
            entity.Property(e => e.OldPassword).HasMaxLength(50);
            entity.Property(e => e.PasswordRenewDate).HasColumnType("datetime");
            entity.Property(e => e.UserName).HasMaxLength(50);
            // SEC-01.01 — widened for PBKDF2$v1$… hashes (run SQL alter before bulk migrate)
            entity.Property(e => e.UserPassword).HasMaxLength(500);
            entity.Property(e => e.UserPhoto).HasMaxLength(250);
            entity.Property(e => e.UserStatus).HasDefaultValue(true);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.PasswordResetTokenId);
            entity.ToTable("PasswordResetToken");
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.UsedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<ConsentType>(entity =>
        {
            entity.HasKey(e => e.ConsentTypeId);
            entity.ToTable("ConsentType");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<ConsentRecord>(entity =>
        {
            entity.HasKey(e => e.ConsentRecordId);
            entity.ToTable("ConsentRecord");
            entity.Property(e => e.SubjectType).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.GrantedAt).HasColumnType("datetime");
            entity.Property(e => e.WithdrawnAt).HasColumnType("datetime");
            entity.HasOne(d => d.ConsentType).WithMany(p => p.ConsentRecords)
                .HasForeignKey(d => d.ConsentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsentRecord_ConsentType");
            entity.HasIndex(e => new { e.SubjectType, e.SubjectId });
        });

        modelBuilder.Entity<OtpChallenge>(entity =>
        {
            entity.HasKey(e => e.OtpChallengeId);
            entity.ToTable("OtpChallenge");
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.DestinationMasked).HasMaxLength(100);
            entity.Property(e => e.OtpHash).HasMaxLength(128);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.LockedUntil).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.VerifiedAt).HasColumnType("datetime");
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.Action });
        });

        modelBuilder.Entity<OtpAuditLog>(entity =>
        {
            entity.HasKey(e => e.OtpAuditLogId);
            entity.ToTable("OtpAuditLog");
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.ToMasked).HasMaxLength(100);
            entity.Property(e => e.At).HasColumnType("datetime");
            entity.HasIndex(e => e.At);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.AuditEventId);
            entity.ToTable("AuditEvent");
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.Entity).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.At).HasColumnType("datetime");
            entity.HasIndex(e => e.At);
            entity.HasIndex(e => e.ActorUserId);
        });

        modelBuilder.Entity<SecureDocument>(entity =>
        {
            entity.HasKey(e => e.SecureDocumentId);
            entity.ToTable("SecureDocument");
            entity.Property(e => e.OwnerType).HasMaxLength(50);
            entity.Property(e => e.BlobPath).HasMaxLength(1000);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.Mime).HasMaxLength(100);
            entity.Property(e => e.Hash).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(e => new { e.OwnerType, e.OwnerId });
        });

        modelBuilder.Entity<YearMaster>(entity =>
        {
            entity.HasKey(e => e.YearId).HasName("PK_YearMasters");

            entity.ToTable("YearMaster");

            entity.Property(e => e.ChangedBy).HasMaxLength(50);
            entity.Property(e => e.ChangedDate).HasColumnType("datetime");
            entity.Property(e => e.DisplayYear).HasMaxLength(25);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.EnteredBy).HasMaxLength(50);
            entity.Property(e => e.EnteredDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.YearType).HasMaxLength(50);

            entity.HasOne(d => d.Firm).WithMany(p => p.YearMasters)
                .HasForeignKey(d => d.FirmId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_YearMaster_FirmDetails");
        });

//OnModelCreatingPartial(modelBuilder);
    }

   // partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
       public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            int userId = 1;//_currentUserService.getUserId();
            foreach (var entry in ChangeTracker.Entries<AuditableEntities>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.EnteredBy = userId.ToString();
                        entry.Entity.EnteredDate = DateTime.Now;//_dateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.ChangedBy = userId.ToString();
                        entry.Entity.ChangedDate = DateTime.Now;
                        break;
                }
            }
            OnBeforeSaveChanges(userId);
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void OnBeforeSaveChanges(int userId)
        {
            ChangeTracker.DetectChanges();
        }

}
}