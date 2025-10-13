using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine
{
    /// <summary>
    /// Trigger condition that count the amount of cards in target's pile of your choise (deck/discard/hand/board...)
    /// Can also only count cards of a specific type/team/trait
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CountTarget", order = 10)]
    public class ConditionCountTarget : ConditionData
    {
        [Header("Count cards of type")]
        public PileType pile;
        public ConditionOperatorInt oper;
        public int value;

        [Header("Traits")]
        public ItemType has_type;
        public TeamData has_team;
        public TraitData has_trait;

        public override bool IsTargetConditionMet(Battle data, AbilityData ability, BattleCharacter caster, Card card, BattleCharacter target)
        {
            int count = CountPile(target, pile);
            return CompareInt(count, oper, value);
        }

        private int CountPile(BattleCharacter player, PileType pile)
        {
            List<Card> card_pile = null;

            if (pile == PileType.Hand)
                card_pile = player.cards_hand;

            if (pile == PileType.Power)
                card_pile = player.cards_power;

            if (pile == PileType.Deck)
                card_pile = player.cards_deck;

            if (pile == PileType.Discard)
                card_pile = player.cards_discard;

            if (pile == PileType.Void)
                card_pile = player.cards_void;

            if (pile == PileType.Temp)
                card_pile = player.cards_temp;

            if (card_pile != null)
            {
                int count = 0;
                foreach (Card card in card_pile)
                {
                    if (IsTrait(card))
                        count++;
                }
                return count;
            }
            return 0;
        }

        private bool IsTrait(Card card)
        {
            bool is_type = card.CardData.item_type == has_type || has_type == ItemType.None;
            bool is_team = card.CardData.team == has_team || has_team == null;
            bool is_trait = card.HasTrait(has_trait) || has_trait == null;
            return (is_type && is_team && is_trait);
        }
    }
}