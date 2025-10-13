using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine.Client
{
    /// <summary>
    /// TutoStep groups do NOT need to be triggered in order, group will be triggered on start_trigger condition 
    /// and then all TutoStep inside group will be executed in order.
    /// </summary>

    public class TutoMapStepGroup : MonoBehaviour
    {
        public string group_id;
        public int depth_min = 1;
        public int depth_max = 99;
        public ChampionData[] champions;
        public TutoMapStartTrigger start_trigger;
        public EventData start_target;
        public bool forced; //Must finish all TutoStep inside group before triggering another group

        private bool triggered = false;

        private static List<TutoMapStepGroup> groups = new List<TutoMapStepGroup>();

        protected virtual void Awake()
        {
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

        public static TutoMapStepGroup Get(TutoMapStartTrigger trigger, int depth, Champion champion = null, EventData evt = null)
        {
            foreach (TutoMapStepGroup s in groups)
            {
                if (s.start_trigger == trigger && !s.triggered)
                {
                    if (depth >= s.depth_min && depth <= s.depth_max)
                    {
                        if (champion == null || s.champions.Length == 0 || s.HasChampion(champion.character_id))
                        {
                            if (evt == null || s.start_target == null || evt.id == s.start_target.id)
                                return s;
                        }
                    }
                }
            }
            return null;
        }

    }
}
