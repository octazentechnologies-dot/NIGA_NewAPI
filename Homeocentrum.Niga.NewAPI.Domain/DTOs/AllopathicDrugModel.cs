using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class AllopathicDrugModel
    {
        public AllopathicDrugModel()
        {
            AdverseReactionModelList = new List<AdverseReactionModel>();
            OtherSideEffectModelList = new List<OtherSideEffectModel>();
            SeriousSideEffectModelList = new List<SeriousSideEffectModel>();
        }

        public int AllopathicDrugId { get; set; }
        public int DrugGroupId { get; set; }
        public string DrugGroupName { get; set; }
        public string AllopathicDrugName { get; set; }
        public bool? DeleteStatus { get; set; }

        public int DrugSystemId { get; set; }
        public string DrugSystemName { get; set; }

        public virtual List<AdverseReactionModel> AdverseReactionModelList { get; set; }
        public virtual List<OtherSideEffectModel> OtherSideEffectModelList { get; set; }
        public virtual List<SeriousSideEffectModel> SeriousSideEffectModelList { get; set; }
        public int? TotalAdverseReactions { get; set; }
        public int? TotalOtherSideEffects { get; set; }
        public int? TotalSeriousSideEffects { get; set; }
    }

    public class AllopathicDrugViewModel
    {
        public int AllopathicDrugId { get; set; }
        public int DrugGroupId { get; set; }
        public string DrugGroupName { get; set; }
        public string AllopathicDrugName { get; set; }
    }
}
