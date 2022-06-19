using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BloodMage
{
    class DamageManager : MonoBehaviour
    {
        public static DamageManager Instance;

        public int HypovolemiaID = -28001;

        private float BleedDamageReduction(float _damage, Character character)
        {
            if(character.Inventory.SkillKnowledge.IsItemLearned(DamageManager.Instance.HypovolemiaID))
            {
                return (float) 0.5 * _damage;
            }
            else
            {
                return _damage;
            }
        }
    }
}
