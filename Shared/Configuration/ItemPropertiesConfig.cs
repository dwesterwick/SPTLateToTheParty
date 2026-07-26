using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace LateToTheParty.Configuration
{
    [DataContract]
    public class ItemPropertiesConfig
    {
        [DataMember(Name = "width", IsRequired = true)]
        public int Width { get; set; }

        [DataMember(Name = "height", IsRequired = true)]
        public int Height { get; set; }

        [DataMember(Name = "weight", IsRequired = true)]
        public double Weight { get; set; }

        public int Size => Width * Height;
        public int MaxDim => Math.Max(Width, Height);

        public ItemPropertiesConfig()
        {

        }

        public ItemPropertiesConfig(int width, int height, double weight)
        {
            Width = width;
            Height = height;
            Weight = weight;
        }
    }
}
