using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine.Client
{
    /// <summary>
    /// TutoStep groups do NOT need to be triggered in order, group will be triggered on start_trigger condition 
    /// and then all TutoStep inside group will be executed in order.
    /// </summary>

    public class TutoStepGroup : MonoBehaviour
    {
        public int turn_min = 0;
        public int turn_max = 99;
        public ChampionData[] champions;
        public TutoStartTrigger start_trigger;
        public CardData start_target;
        public bool forced; //Must finish all TutoStep inside group before triggering another group

        private int step;
        private bool triggered = false;

        private static List<TutoStepGroup> groups = new List<TutoStepGroup>();

        protected virtual void Awake()
        {
            step = transform.GetSiblingIndex();
            groups.Add(this);
        }

        protected virtual void OnDestroy()
        {
            groups.Remove(this);
        }

        public void SetTriggered()
        {
            triggered = true;
        }

        public bool HasChampion(string champion_id)
        {
            foreach (ChampionData champ in champions)
            {
                if (champ.id == champion_id)
                    return true;
            }
            return false;
        }

        public static TutoStepGroup Get(TutoStartTrigger trigger, BattleCharacter character, int turn)
        {
            foreach (TutoStepGroup s in groups)
            {
                if (s.start_trigger == trigger && !s.triggered)
                {
                    if (turn >= s.turn_min && turn <= s.turn_max)
                    {
                        if(s.champions.Length == 0 || s.HasChampion(character.character_id))
                            return s;
                    }
                }
            }
            return null;
        }

        public static TutoStepGroup Get(TutoStartTrigger trigger, BattleCharacter character, Card target, int turn)
        {
            foreach (TutoStepGroup s in groups)
            {
                if (s.start_trigger == trigger && !s.triggered)
                {
                    if (turn >= s.turn_min && turn <= s.turn_max)
                    {
                        if (s.champions.Length == 0 || s.HasChampion(character.character_id))
                        {
                            if (s.start_target == null || target == null || s.start_target.id == target.card_id)
                                return s;
                        }
                    }
                }
            }
            return null;
        }

    }
}
