using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class SeriousSideEffectModel
    {
        public int SeriousSideEffectId { get; set; }
        public string SeriousSideEffectName { get; set; }
        public int AllopathicDrugId { get; set; }
        public string AllopathicDrugName { get; set; }
        public bool? DeleteStatus { get; set; }
    }
}
