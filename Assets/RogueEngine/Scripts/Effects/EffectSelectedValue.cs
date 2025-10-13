using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.Gameplay;

namespace RogueEngine
{
    /// <summary>
    /// Add/Reduce selected value
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SelectedValue", order = 10)]
    public class EffectSelectedValue : EffectData
    {
        public EffectOperatorInt oper;
        public int value;

        public override void DoEffect(BattleLogic logic, AbilityData ability, BattleCharacter character, Card card, BattleCharacter target)
        {
            logic.BattleData.selected_value = AddOrSet(logic.BattleData.selected_value, oper, value);
        }

        public override void DoEffect(BattleLogic logic, AbilityData ability, BattleCharacter character, Card card, Card target)
        {
            logic.BattleData.selected_value = AddOrSet(logic.BattleData.selected_value, oper, value);
        }

    }
}