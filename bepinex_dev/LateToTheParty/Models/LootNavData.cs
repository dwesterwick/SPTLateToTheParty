using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LateToTheParty.Models
{
    public class LootNavData
    {
        public bool IsAccessible { get; set; } = false;
        public Vector3 AccessibleFromPosition { get; set; }

        public LootNavData()
        {

        }
    }
}
