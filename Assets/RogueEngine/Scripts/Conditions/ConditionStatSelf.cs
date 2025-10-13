using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine
{
    /// <summary>
    /// Compares basic card or player stats such as hp/mana
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/StatSelf", order = 10)]
    public class ConditionStatSelf : ConditionData
    {
        [Header("Character stat is")]
        public EffectStatType type;
        public ConditionOperatorInt oper;
        public int value;

        public override bool IsMapEventConditionMet(World data, EventEffect evt, Champion champion)
        {
            if (type == EffectStatType.HP)
            {
                return CompareInt(champion.hp, oper, value);
            }

            if (type == EffectStatType.Level)
            {
                return CompareInt(champion.level, oper, value);
            }

            if (type == EffectStatType.XP)
            {
                return CompareInt(champion.xp, oper, value);
            }

            if (type == EffectStatType.Gold)
            {
                Player player = data.GetPlayer(champion.player_id);
                return CompareInt(player.gold, oper, value);
            }

            return false;
        }

        public override bool IsTriggerConditionMet(Battle data, AbilityData ability, BattleCharacter caster, Card card)
        {
            if (type == EffectStatType.HP)
            {
                return CompareInt(caster.hp, oper, value);
            }

            if (type == EffectStatType.Mana)
            {
                return CompareInt(caster.mana, oper, value);
            }

            if (type == EffectStatType.Shield)
            {
                return CompareInt(caster.shield, oper, value);
            }

            return false;
        }

    }
}