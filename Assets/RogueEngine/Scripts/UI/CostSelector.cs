using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace RogueEngine.UI
{
    /// <summary>
    /// The choice selector is a box that appears when using an ability with ChoiceSelector as target
    /// it let you choose between different abilities
    /// </summary>

    public class CostSelector : SelectorPanel
    {
        public NumberSelector selector;

        private Card card;

        private static CostSelector instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

        }

        protected override void Update()
        {
            base.Update();

            Battle game = GameManager.Get().GetBattle();
            if (game != null && game.selector == SelectorType.None)
                Hide();
        }

        public void RefreshPanel()
        {
            if (card == null)
                return;

            Battle game = GameManager.Get().GetBattle();
            BattleCharacter character = game.GetCharacter(card.owner_uid);
            selector.SetMax(character.mana);
            selector.SetValue(0);
        }

        public void OnClickOK()
        {
            Battle data = GameManager.Get().GetBattle();
            if (data.selector == SelectorType.SelectorCost)
            {
                GameManager.Get().SelectCost(selector.value);
            }

            Hide();
        }

        public void OnClickCancel()
        {
            GameManager.Get().CancelSelection();
            Hide();
        }

        public override void Show(AbilityData iability, BattleCharacter caster, Card card)
        {
            this.card = card;
            Show();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshPanel();
        }

        public override bool ShouldShow()
        {
            Battle data = GameManager.Get().GetBattle();
            int player_id = GameManager.Get().GetPlayerID();
            return data.selector == SelectorType.SelectorCost && data.selector_player_id == player_id;
        }

        public static CostSelector Get()
        {
            return instance;
        }
    }
}
