using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.UI;

namespace RogueEngine.Client
{
    /// <summary>
    /// TutoStep inside a group will all be triggered sequentially, game proceed to next step when end_trigger is met, or when another group is triggered
    /// </summary>

    public class TutoMapStep : UIPanel
    {
        [Header("Tuto Step")]
        public TutoMapEndTrigger end_trigger;
        public CardData trigger_target;
        public bool forced;                //Player MUST do the end_trigger action to proceed

        private TutoMapStepGroup group;
        private int step;
        private TutoBox tuto_box;

        private static List<TutoMapStep> steps = new List<TutoMapStep>();

        protected override void Awake()
        {
            base.Awake();
            step = transform.GetSiblingIndex();
            group = GetComponentInParent<TutoMapStepGroup>();
            tuto_box = GetComponentInChildren<TutoBox>();
            steps.Add(this);
        }

        protected virtual void OnDestroy()
        {
            steps.Remove(this);
        }

        protected override void Start()
        {
            base.Start();
            tuto_box.SetNextButton(end_trigger == TutoMapEndTrigger.Click);
        }

        public int GetStepIndex()
        {
            return step;
        }

        public static TutoMapStep Get(TutoMapStepGroup group, int step)
        {
            foreach (TutoMapStep s in steps)
            {
                if (s.group == group && s.step == step)
                    return s;
            }
            return null;
        }

        public static void HideAll()
        {
            foreach (TutoMapStep s in steps)
            {
                s.Hide();
            }
        }

    }

}
