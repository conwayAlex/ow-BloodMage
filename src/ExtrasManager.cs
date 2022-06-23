using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace BloodMage
{
    class ExtrasManager : MonoBehaviour
    {
        public static ExtrasManager Instance;

        internal void Awake()
        {
            ExtrasManager.Instance = this;
        }

        public int HumoursMaintenance = -28002;



    }
}
