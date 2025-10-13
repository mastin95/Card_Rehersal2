using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.Gameplay;

namespace RogueEngine
{
    /// <summary>
    /// Repeat an ability X times
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Repeat", order = 10)]
    public class EffectRepeat : EffectData
    {
        public AbilityData ability;
        public EffectRepeatType type;

        public override void DoEffect(BattleLogic logic, AbilityData iability, BattleCharacter character, Card card)
        {
            int count = GetRepeatCount(logic.BattleData, iability);
            for (int i = 0; i < count; i++)
            {
                logic.TriggerAbilityDelayed(this.ability, character, card);
            }
        }

        public override void DoEffect(BattleLogic logic, AbilityData iability, BattleCharacter character, Card card, BattleCharacter target)
        {
            int count = GetRepeatCount(logic.BattleData, iability);
            for (int i = 0; i < count; i++)
            {
                logic.TriggerAbilityDelayed(this.ability, character, card);
            }
        }

        public override void DoEffect(BattleLogic logic, AbilityData iability, BattleCharacter character, Card card, Card target)
        {
            int count = GetRepeatCount(logic.BattleData, iability);
            for (int i = 0; i < count; i++)
            {
                logic.TriggerAbilityDelayed(this.ability, character, card);
            }
        }

        public int GetRepeatCount(Battle battle, AbilityData iability)
        {
            if (type == EffectRepeatType.SelectedValue)
                return battle.selected_value;
            if (type == EffectRepeatType.FixedValue)
                return iability.value;
            return 0;
        }
    }


    public enum EffectRepeatType
    {
        FixedValue,
        SelectedValue
    }
}