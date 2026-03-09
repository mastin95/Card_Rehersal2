using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


namespace RogueEngine.UI
{

    public class EventChoicePanel : MapPanel
    {
        public Text text;
        public EventChoiceLine[] lines;

        private bool skip_choice;

        private static EventChoicePanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            foreach (EventChoiceLine line in lines)
                line.onClick += OnClickChoice; // Changed to OnClickChoice
        }

        public void ShowText(string text)
        {
            skip_choice = true;
            this.text.text = text;

            foreach (EventChoiceLine line in lines)
                line.Hide();

            lines[0].SetText(0, "OK");
        }

        public void OnClickContinue() // Added this method
        {
            GameManager.Get().MapEventContinue();
        }

        public void ShowChoices(EventChoice evt)
        {
            skip_choice = false;
            text.text = evt.GetText();

            foreach (EventChoiceLine line in lines)
                line.Hide();

            World world = GameManager.Get().GetWorld();
            int player_id = GameManager.Get().GetPlayerID();
            Champion champ = world.GetFirstChampion(player_id);

            int index = 0;
            foreach (ChoiceElement choice in evt.choices)
            {
                // The instruction provided a malformed if statement and a problematic line.
                // Based on the instruction's intent to use 'achoice' for condition checking,
                // and assuming 'choice' refers to 'ChoiceElement' within the loop,
                // and that 'ChoiceElement' should have a way to get its 'EventChoiceData',
                // I'm interpreting 'choice.GetChoice(index)' as 'choice.data'.
                // The malformed 'if (achoice != null) if (...)' is corrected to a single 'if' statement.
                EventData achoice = choice.effect;
                if (achoice != null && index < lines.Length && choice.effect.AreEventsConditionMet(world, champ))
                {
                    lines[index].SetLine(index, choice);
                    index++;
                }
            }
        }

        public override void RefreshPanel()
        {
            World world = GameManager.Get().GetWorld();
            if (world.state == WorldState.EventChoice)
            {
                EventChoice choice = EventChoice.Get(world.event_id);
                ShowChoices(choice);
            }
            else if (world.state == WorldState.EventText)
            {
                ShowText(world.event_text);
            }
        }

        public override bool ShouldShow()
        {
            World world = GameManager.Get().GetWorld();
            EventChoice choice = EventChoice.Get(world.event_id);
            return (world.state == WorldState.EventChoice && choice != null) || world.state == WorldState.EventText;
        }

        public override bool ShouldRefresh()
        {
            return false;
        }

        public override bool IsAutomatic()
        {
            return true;
        }

        public void OnClickChoice(EventChoiceLine line) // Renamed from OnClick, made public
        {
            // Removed tutorial logic
            // if (TutorialMap.IsTuto() && !TutorialMap.Get().CanDo(TutoMapEndTrigger.SelectChoice))
            //    return;

            World world = GameManager.Get().GetWorld();
            int player_id = GameManager.Get().GetPlayerID();
            Champion champ = world.GetFirstChampion(player_id);
            if (champ != null)
            {
                if(skip_choice)
                    GameManager.Get().MapEventContinue();
                else
                {
                    EventData selected_choice = line.GetEvent(); // Corrected this line based on snippet intent
                    GameManager.Get().MapSelectChoice(champ, selected_choice); // Used selected_choice
                }
            }
        }

        public static EventChoicePanel Get()
        {
            return instance;
        }
    }
}
