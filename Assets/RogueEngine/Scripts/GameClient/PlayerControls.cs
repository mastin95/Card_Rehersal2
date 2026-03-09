using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RogueEngine.UI;

namespace RogueEngine
{
    /// <summary>
    /// Script that contain main controls for clicking on cards, attacking, activating abilities
    /// Will send action to GameClient on click release
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        private BoardCharacter selected_character = null;

        private static PlayerControls instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!GameManager.Get().IsReady())
                return;

            if (MouseInput.IsRightClick())
                MouseRightClick();

            if (selected_character != null)
            {
                if (MouseInput.IsLeftRelease())
                    ReleaseClick();
            }
        }

        public void SelectCharacter(BoardCharacter bcard)
        {
            Battle gdata = GameManager.Get().GetBattle();
            if (gdata == null)
                return;

            Player player = GameManager.Get().GetPlayer();
            BattleCharacter character = bcard.GetCharacter();

            if (gdata.IsPlayerSelectorTurn(player.player_id) && gdata.selector == SelectorType.SelectTarget)
            {
                GameManager.Get().SelectCharacter(character);
            }
            else if (gdata.IsPlayerActionTurn(player.player_id) && gdata.CanControlCharacter(player.player_id, character))
            {
                selected_character = bcard;
            }
        }

        public void MouseRightClick()
        {
            UnselectAll();
        }

        private void ReleaseClick()
        {
            UnselectAll();
        }

        public void UnselectAll()
        {
            selected_character = null;
        }

        public BoardCharacter GetSelected()
        {
            return selected_character;
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }
}
