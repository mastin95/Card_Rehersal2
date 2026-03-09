using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine
{

    public class Tutorial : MonoBehaviour
    {

        private bool is_tuto = false;
        private TutoStepGroup current_group;
        private TutoStep current_step;
        private bool locked = false;

        private static Tutorial instance;

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            World world = GameManager.Get().GetWorld();
            if (world != null && world.state == WorldState.Battle && world.battle != null)
            {
                is_tuto = true;
                GameManager.Get().onNewTurn += OnNewTurn;
                GameManager.Get().onCardPlayed += OnCardPlayed;
                GameManager.Get().onAbilityTargetCharacter += OnTargetCharacter;
                GameManager.Get().onAbilityTargetCard += OnTargetCard;
                HideAll();
            }
        }

        private void OnDestroy()
        {
            if (is_tuto)
            {
                GameManager.Get().onNewTurn -= OnNewTurn;
                GameManager.Get().onCardPlayed -= OnCardPlayed;
                GameManager.Get().onAbilityTargetCharacter -= OnTargetCharacter;
                GameManager.Get().onAbilityTargetCard -= OnTargetCard;
            }
        }

        private void OnNewTurn()
        {
            Battle data = GameManager.Get().GetBattle();
            if (data == null)
                return;

            EndGroup();

            int player_id = GameManager.Get().GetPlayerID();
            BattleCharacter character = data.GetActiveCharacter();
            if (character == null || !data.CanControlCharacter(player_id, character))
                return;

            TutoStepGroup group = TutoStepGroup.Get(TutoStartTrigger.StartTurn, character, data.turn_count);
            ShowGroup(group);
        }

        private void OnCardPlayed(Card card, Slot slot)
        {
            int player_id = GameManager.Get().GetPlayerID();
            Battle data = GameManager.Get().GetBattle();
            if (data.CanControlCard(player_id, card))
            {
                TriggerEndStep(TutoEndTrigger.PlayCard);
                TriggerStartGroup(TutoStartTrigger.PlayCard, card);
            }
        }

        private void OnTargetCard(AbilityData ability, Card card, Card target)
        {
            int player_id = GameManager.Get().GetPlayerID();
            Battle data = GameManager.Get().GetBattle();
            if (data.CanControlCard(player_id, card))
            {
                TriggerEndStep(TutoEndTrigger.SelectTarget);
            }
        }

        private void OnTargetCharacter(AbilityData ability, Card card, BattleCharacter target)
        {
            int player_id = GameManager.Get().GetPlayerID();
            Battle data = GameManager.Get().GetBattle();
            if (data.CanControlCard(player_id, card))
            {
                TriggerEndStep(TutoEndTrigger.SelectTarget);
            }
        }

        public void TriggerEndStep(TutoEndTrigger trigger, float time = 1f)
        {
            if (current_step != null && current_step.end_trigger == trigger)
            {
                Hide();

                TutoStepGroup group = current_group;
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

        public void TriggerStartGroup(TutoStartTrigger trigger, Card target = null)
        {
            if (current_group == null || !current_group.forced)
            {
                if (current_step == null || !current_step.forced)
                {
                    ShowGroup(trigger, target);
                }
            }
        }

        public void ShowGroup(TutoStartTrigger trigger, Card target)
        {
            Battle data = GameManager.Get().GetBattle();
            BattleCharacter character = data.GetActiveCharacter();
            TutoStepGroup group = TutoStepGroup.Get(trigger, character, target, data.turn_count);
            ShowGroup(group);
        }

        public void ShowGroup(TutoStepGroup group)
        {
            if (group != null)
            {
                current_group = group;
                group.SetTriggered();
                TutoStep step = TutoStep.Get(group, 0);
                Show(step);
            }
        }

        public void ShowNext()
        {
            if (current_group != null)
            {
                int index = GetNextIndex();
                TutoStep step = TutoStep.Get(current_group, index);
                if (step != null)
                    Show(step);
                else
                    EndGroup();
            }
        }

        public void Show(TutoStep step)
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

        public void Hide(TutoStep step)
        {
            if (step != null)
                step.Hide();
        }

        public void Hide()
        {
            Hide(current_step);
        }

        public bool CanDo(TutoEndTrigger trigger)
        {
            return CanDo(trigger, Slot.None);
        }

        public bool CanDo(TutoEndTrigger trigger, Slot slot)
        {
            Battle data = GameManager.Get().GetBattle();
            BattleCharacter target = data.GetSlotCharacter(slot);
            return CanDo(trigger, target);
        }

        public bool CanDo(TutoEndTrigger trigger, BattleCharacter target)
        {
            if (!is_tuto)
                return true; //Not a tutorial

            if (locked)
                return false;

            if (current_step != null && current_step.forced)
            {
                if (trigger == TutoEndTrigger.PlayCard && current_step.end_trigger == TutoEndTrigger.SelectTarget)
                    return true; //Dont get locked into select target if ability was canceled

                if (current_step.end_trigger != trigger)
                    return false; //Wrong trigger

                CharacterData target_data = target != null ? target.CharacterData : null;
                if (current_step.trigger_target != null && current_step.trigger_target != target_data)
                    return false; //Wrong target
            }

            return true;
        }

        public bool CanDo(TutoEndTrigger trigger, Card target)
        {
            if (!is_tuto)
                return true; //Not a tutorial

            if (locked)
                return false;

            if (current_step != null && current_step.forced)
            {
                if (trigger == TutoEndTrigger.PlayCard && current_step.end_trigger == TutoEndTrigger.SelectTarget)
                    return true; //Dont get locked into select target if ability was canceled

                if (current_step.end_trigger != trigger)
                    return false; //Wrong trigger

                CardData target_data = target != null ? target.CardData : null;
                if (current_step.trigger_target_card != null && current_step.trigger_target_card != target_data)
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

        public TutoEndTrigger GetEndTrigger()
        {
            if (current_step != null)
                return current_step.end_trigger;
            return TutoEndTrigger.Click;
        }

        public void HideAll()
        {
            TutoStep.HideAll();
        }

        public static bool IsTuto()
        {
            return instance != null && instance.is_tuto;
        }

        public static Tutorial Get()
        {
            return instance;
        }
    }

    public enum TutoStartTrigger
    {
        StartTurn = 0,
        PlayCard = 10,
    }

    public enum TutoEndTrigger
    {
        Click = 0,
        EndTurn = 5,
        PlayCard = 10,
        Move = 20,
        SelectTarget = 30,
    }
}
