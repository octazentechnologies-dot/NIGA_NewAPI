using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class DiagnosisSearchResultModel
    {

        public DiagnosisSearchResultModel() {
            diagnosisRemediesModels = new List<DiagnosisRemediesModel>();
            DiagnosisSystemList = new List<DiagnosisSystemViewModel>();
        }
        public int DiagnosisID { get; set; } = 0;
        public string DiagnosisName { get; set; }=string.Empty;
        public string DiagnosisNameAlias { get; set; }=string.Empty;
        public string Miasm { get; set; }=string.Empty;
        public string Investigations { get; set; }=string.Empty;
        public string AllopathicMedicines { get; set; }=string.Empty;
        public string Examiniations { get; set; }=string.Empty;
        public string Therapeutics { get; set; }=string.Empty;

       public List<DiagnosisRemediesModel> diagnosisRemediesModels { get; set; }
       public List<DiagnosisSystemViewModel> DiagnosisSystemList { get; set; }


    }

    public class DiagnosisSystemViewModel
    {
        public int DiagnosisSystemID { get; set; } = 0;
        public string DiagnosisSystemName { get; set; } = string.Empty;
    }

}
