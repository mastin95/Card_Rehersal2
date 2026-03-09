using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RogueEngine;

namespace RogueEngine.UI
{
    /// <summary>
    /// Box that appears when using the SelectTarget ability target
    /// </summary>

    public class SelectTargetUI : SelectorPanel
    {
        public Text title;
        public Text desc;

        private static SelectTargetUI _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            Hide(true);
        }

        protected override void Update()
        {
            base.Update();

            Battle game = GameManager.Get().GetBattle();
            if (game != null && game.selector == SelectorType.None)
                Hide();
        }

        public override void Show(AbilityData iability, BattleCharacter caster, Card card)
        {
            title.text = iability.title;
            Show();
        }

        public void OnClickClose()
        {
            GameManager.Get().CancelSelection();
        }

        public override bool ShouldShow()
        {
            Battle battle = GameManager.Get().GetBattle();
            int player_id = GameManager.Get().GetPlayerID();
            return battle.selector == SelectorType.SelectTarget && battle.selector_player_id == player_id;
        }

        public static SelectTargetUI Get()
        {
            return _instance;
        }
    }
}
