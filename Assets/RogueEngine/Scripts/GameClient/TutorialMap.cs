using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine.Client
{

    public class TutorialMap : MonoBehaviour
    {

        private bool is_tuto = false;
        private TutoMapStepGroup current_group;
        private TutoMapStep current_step;
        private bool locked = false;
        private WorldState prev_state = WorldState.None;

        private static TutorialMap instance;

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            is_tuto = true;
            GameClient.Get().onMapMove += OnMapMove;
            GameClient.Get().onEventStart += OnEventStart;
            GameClient.Get().onEventChoice += OnSelectChoice;
            GameClient.Get().onRewardChoice += OnSelectReward;
            GameClient.Get().onUpgradeCard += OnUpgradeCard;
            GameClient.Get().onBuyCard += OnBuyCard;
            GameClient.Get().onBuyItem += OnBuyItem;
            HideAll();
        }

        private void OnDestroy()
        {
            GameClient.Get().onMapMove -= OnMapMove;
            GameClient.Get().onEventStart -= OnEventStart;
            GameClient.Get().onEventChoice -= OnSelectChoice;
            GameClient.Get().onRewardChoice -= OnSelectReward;
            GameClient.Get().onUpgradeCard -= OnUpgradeCard;
            GameClient.Get().onBuyCard -= OnBuyCard;
            GameClient.Get().onBuyItem -= OnBuyItem;
        }

        private void Update()
        {
            World world = GameClient.Get().GetWorld();
            if (world != null)
            {
                if (world.state != prev_state)
                {
                    prev_state = world.state;
                    CheckMapState();
                }
            }
        }

        private void CheckMapState()
        {
            World world = GameClient.Get().GetWorld();

            if (world.state == WorldState.Map)
            {
                TutoMapStepGroup group = TutoMapStepGroup.Get(TutoMapStartTrigger.MapView, world.GetCurrentLocationDepth());
                ShowGroup(group);
            }

            if (world.state == WorldState.Reward)
            {
                Champion champion = world.GetFirstChampion(GameClient.Get().GetPlayerID());
                TutoMapStepGroup group = TutoMapStepGroup.Get(TutoMapStartTrigger.CardReward, world.GetCurrentLocationDepth(), champion);
                ShowGroup(group);
            }

            if (world.state == WorldState.EventChoice || world.state == WorldState.EventText)
            {
                Champion champion = world.GetChampion(world.event_champion);
                EventData evt = EventData.Get(world.event_id);
                TutoMapStepGroup group = TutoMapStepGroup.Get(TutoMapStartTrigger.EventStart, world.GetCurrentLocationDepth(), champion, evt);
                ShowGroup(group);
            }
        }

        private void OnMapMove(Champion champion, MapLocation loc)
        {
            EventData evt = loc.GetEvent();
            TriggerEndStep(TutoMapEndTrigger.Move);
            TriggerStartGroup(TutoMapStartTrigger.Move, loc.depth, champion, evt);
        }

        private void OnEventStart(Champion champ, EventData evt)
        {
            World world = GameClient.Get().GetWorld();
            int player_id = GameClient.Get().GetPlayerID();
            if (world.CanControlChampion(player_id, champ))
            {
                TriggerStartGroup(TutoMapStartTrigger.EventStart, world.GetCurrentLocationDepth(), champ, evt);
            }
        }

        private void OnSelectChoice(Champion champ, EventData evt)
        {
            World world = GameClient.Get().GetWorld();
            int player_id = GameClient.Get().GetPlayerID();
            if (world.CanControlChampion(player_id, champ))
            {
                TriggerEndStep(TutoMapEndTrigger.SelectChoice);
            }
        }

        private void OnSelectReward(Champion champ, CardData card)
        {
            World world = GameClient.Get().GetWorld();
            int player_id = GameClient.Get().GetPlayerID();
            if (world.CanControlChampion(player_id, champ))
            {
                TriggerEndStep(TutoMapEndTrigger.SelectChoice);
            }
        }

        private void OnUpgradeCard(Champion champ, CardData card)
        {
            World world = GameClient.Get().GetWorld();
            int player_id = GameClient.Get().GetPlayerID();
            if (world.CanControlChampion(player_id, champ))
            {
                TriggerEndStep(TutoMapEndTrigger.Upgrade);
            }
        }

        private void OnBuyCard(Champion champ, CardData card)
        {
            World world = GameClient.Get().GetWorld();
            int player_id = GameClient.Get().GetPlayerID();
            if (world.CanControlChampion(player_id, champ))
            {
                TriggerEndStep(TutoMapEndTrigger.Buy);
            }
        }

        private void OnBuyItem(Champion champ, CardData card)
        {
            World world = GameClient.Get().GetWorld();
            int player_id = GameClient.Get().GetPlayerID();
            if (world.CanControlChampion(player_id, champ))
            {
                TriggerEndStep(TutoMapEndTrigger.Buy);
            }
        }

        public void TriggerEndStep(TutoMapEndTrigger trigger, float time = 1f)
        {
            if (current_step != null && current_step.end_trigger == trigger)
            {
                Hide();

                TutoMapStepGroup group = current_group;
                locked = true;
                TimeTool.WaitFor(time, () =>
                {
                    locked = false;
                    if (group == current_group)
                    {
                        ShowNext();
                    }
                });
            }
        }

        public void TriggerStartGroup(TutoMapStartTrigger trigger, int depth, Champion champion, EventData target = null)
        {
            if (current_group == null || !current_group.forced)
            {
                if (current_step == null || !current_step.forced)
                {
                    ShowGroup(trigger, depth, champion,target);
                }
            }
        }

        public void ShowGroup(TutoMapStartTrigger trigger, int depth, Champion champion, EventData target)
        {
            TutoMapStepGroup group = TutoMapStepGroup.Get(trigger, depth, champion, target);
            ShowGroup(group);
        }

        public void ShowGroup(TutoMapStepGroup group)
        {
            if (group != null)
            {
                current_group = group;
                group.SetTriggered();
                TutoMapStep step = TutoMapStep.Get(group, 0);
                Show(step);
            }
        }

        public void ShowNext()
        {
            if (current_group != null)
            {
                int index = GetNextIndex();
                TutoMapStep step = TutoMapStep.Get(current_group, index);
                if (step != null)
                    Show(step);
                else
                    EndGroup();
            }
        }

        public void Show(TutoMapStep step)
        {
            HideAll();
            current_step = step;
            if (step != null)
                step.Show();
        }

        public void EndGroup()
        {
            HideAll();
            current_group = null;
            current_step = null;
        }

        public void Hide(TutoMapStep step)
        {
            if (step != null)
                step.Hide();
        }

        public void Hide()
        {
            Hide(current_step);
        }

        public bool CanDo(TutoMapEndTrigger trigger)
        {
            return CanDo(trigger, null);
        }

        public bool CanDo(TutoMapEndTrigger trigger, CardData target)
        {
            if (!is_tuto)
                return true; //Not a tutorial

            if (locked)
                return false;

            if (current_step != null && current_step.forced)
            {
                if (current_step.end_trigger != trigger)
                    return false; //Wrong trigger

                if (current_step.trigger_target != null && current_step.trigger_target != target)
                    return false; //Wrong target
            }

            return true;
        }

        public int GetNextIndex()
        {
            if (current_step != null)
                return current_step.GetStepIndex() + 1;
            return 0;
        }

        public TutoMapEndTrigger GetEndTrigger()
        {
            if (current_step != null)
                return current_step.end_trigger;
            return TutoMapEndTrigger.Click;
        }

        public void HideAll()
        {
            TutoMapStep.HideAll();
        }

        public static bool IsTuto()
        {
            return instance != null && instance.is_tuto;
        }

        public static TutorialMap Get()
        {
            return instance;
        }
    }

    public enum TutoMapStartTrigger
    {
        MapView = 0,
        Move = 10,
        EventStart = 15,
        CardReward = 20,
    }

    public enum TutoMapEndTrigger
    {
        Click = 0,
        Move = 10,
        SelectChoice = 15,
        Upgrade = 20,
        Buy = 22,
        Sell = 23,
    }
}
