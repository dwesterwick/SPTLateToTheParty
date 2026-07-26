using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Models
{
    [DataContract]
    public class LootRankingDataConfig
    {
        [DataMember(Name = "id", IsRequired = true)]
        public string ID { get; set; } = "";

        [DataMember(Name = "name", IsRequired = true)]
        public string Name { get; set; } = "";

        [DataMember(Name = "value", IsRequired = true)]
        public double Value { get; set; }

        [DataMember(Name = "costPerSlot", IsRequired = true)]
        public double CostPerSlot { get; set; }

        [DataMember(Name = "weight", IsRequired = true)]
        public double Weight { get; set; }

        [DataMember(Name = "size", IsRequired = true)]
        public double Size { get; set; }

        [DataMember(Name = "gridSize", IsRequired = true)]
        public double GridSize { get; set; }

        [DataMember(Name = "armorClass", IsRequired = true)]
        public double ArmorClass { get; set; }

        [DataMember(Name = "maxDim", IsRequired = true)]
        public double MaxDim { get; set; }

        [DataMember(Name = "parentWeighting", IsRequired = true)]
        public double ParentWeighting { get; set; }

        public LootRankingDataConfig()
        {

        }
    }
}
