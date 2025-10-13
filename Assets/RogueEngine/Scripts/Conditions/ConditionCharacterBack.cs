using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.AI;

namespace RogueEngine
{
    /// <summary>
    /// Condition that checks if the character is at the back
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CharacterBack", order = 10)]
    public class ConditionCharacterBack : ConditionData
    {
        [Header("Character is at the back")]
        public ConditionOperatorBool oper;

        public override bool IsTargetConditionMet(Battle data, AbilityData ability, BattleCharacter caster, Card card, BattleCharacter target)
        {
            BattleCharacter back = data.GetBackCharacter(target.IsEnemy());
            if(back != null)
                return CompareBool(back.uid == target.uid, oper);
            return false;
        }
    }
}