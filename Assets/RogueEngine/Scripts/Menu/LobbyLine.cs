using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RogueEngine.UI
{
    public class LobbyLine : MonoBehaviour
    {
        public Text title;
        public Text subtitle;
        public Text players;
        public Image highlight;

        private LobbyGame game;

        void Awake()
        {

        }

        public void SetLine(LobbyGame room)
        {
            game = room;
            highlight.enabled = false;

            if(title != null)
                title.text = game.title;
            if (subtitle != null)
                subtitle.text = game.subtitle;
            if(players != null)
                players.text = game.players.Count + "/" + game.players_max;
        }

        public void SetSelected(bool selected)
        {
            highlight.enabled = selected;
        }

        public void OnClick()
        {
            LobbyPanel.Get().OnClickLine(this);
        }

        public LobbyGame GetGame()
        {
            return game;
        }
    }
}