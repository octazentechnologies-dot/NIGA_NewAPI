using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
   public class DiagnosisModel
    {
        public DiagnosisModel()
        {
            this.ModelEx = new List<DignosisDetailModel>();
            this.diagnosisCausationList = new List<DiagnosisCausationModel>();
            this.diagnosisMonogramsList = new List<DiagnosisMonogramsModel>();
            this.diagnosisSymptomsList = new List<DiagnosisSymptomsModel>();
            this.diagnosisPathologyList = new List<DiagnosisPathologyModel>();
            this.diagnosisSystemDetailsList = new List<DiagnosisSystemDetailModel>();
            this.emergencieDetailsModelList = new List<EmergencieDetailsModel>();
            this.OnsetDurationProgressDetails = new List<OnsetDurationProgressDetailsModel>();
            this.PatternsDetails = new List<PatternsDetailModel>();
            this.LocationExtentionDetailsModelList = new List<LocationExtentionDetailsModel>();
            this.sensationDetailsModelList = new List<SensationDetailsModel>();
            this.modalitiesDetailsModelsList = new List<ModalitiesDetailsModel>();
            this.accompaniedDetailsModelsList = new List<AccompaniedDetailsModel>();
            this.observationsDetailsModelsList = new List<ObservationsDetailsModel>();
            this.beforeAfterDuringDetailsModelsList = new List<BeforeAfterDuringDetailsModel>();
            this.diagnosisMonogramDetailsModelsList = new List<DiagnosisMonogramDetailsModel>();
            this.diagnosisPathologyDetailsModelsList = new List<DiagnosisPathologyDetailsModel>();
        }
       


        public int DiagnosisId { get; set; }
        public int? DiagnosisGroupId { get; set; }
        [Required(ErrorMessage = "Diagnosis Name is required")]
        public string DiagnosisName { get; set; }
        [Required(ErrorMessage = "Diagnosis Name Alias is required")]
        public string DiagnosisNameAlias { get; set; }
        public string Description { get; set; }
        public string Investigations { get; set; }
        public string AllopathicMedicines { get; set; }
        public string Examiniations { get; set; }
        public string Miasm { get; set; }
        public string Keywords { get; set; }

        
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }

        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public List<DignosisDetailModel> ModelEx { get; set; }
        public List<DiagnosisCausationModel> diagnosisCausationList { get; set; }
        public List<DiagnosisMonogramsModel> diagnosisMonogramsList { get; set; }
        public List<DiagnosisSymptomsModel> diagnosisSymptomsList { get; set; }
        public List<DiagnosisPathologyModel> diagnosisPathologyList { get; set; }
        public List<DiagnosisSystemDetailModel> diagnosisSystemDetailsList { get; set; }
        public List<EmergencieDetailsModel> emergencieDetailsModelList { get; set; }
        public List<OnsetDurationProgressDetailsModel> OnsetDurationProgressDetails { get; set; }
        public List<PatternsDetailModel> PatternsDetails { get; set; }
        public List<LocationExtentionDetailsModel> LocationExtentionDetailsModelList { get; set; }
        public List<SensationDetailsModel> sensationDetailsModelList { get; set; }
        public List<ModalitiesDetailsModel> modalitiesDetailsModelsList { get; set; }
        public List<AccompaniedDetailsModel> accompaniedDetailsModelsList { get; set; }
        public List<ObservationsDetailsModel> observationsDetailsModelsList { get; set; }
        public List<BeforeAfterDuringDetailsModel> beforeAfterDuringDetailsModelsList { get; set; }
        public List<DiagnosisMonogramDetailsModel> diagnosisMonogramDetailsModelsList { get; set; }
        public List<DiagnosisPathologyDetailsModel> diagnosisPathologyDetailsModelsList { get; set; }

    }


    public class DignosisDetailModel
    {
        public int DiagnosisDetailId { get; set; }
        public int? DiagnosisId { get; set; }
        public int? SubSectionId { get; set; }
        public string SubsectionName { get; set; }

        public bool? DeleteStatus { get; set; }
    }

    public class DignosisViewModel
    {
        public int DiagnosisId { get; set; }
        public string DiagnosisName { get; set; }
        public string DiagnosisNameAlias { get; set; }
    }

}
