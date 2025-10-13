using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine
{
    /// <summary>
    /// Check selected value for card cost
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SelectedValue", order = 10)]
    public class ConditionSelectedValue : ConditionData
    {
        [Header("Selected Value is")]
        public ConditionOperatorInt oper;
        public int value;

        public override bool IsTriggerConditionMet(Battle data, AbilityData ability, BattleCharacter character, Card card)
        {
            return CompareInt(data.selected_value, oper, value);
        }
    }
}