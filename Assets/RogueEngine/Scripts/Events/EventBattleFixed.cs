using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueEngine.Gameplay;

namespace RogueEngine
{
    //Battle with Fixed deck

    [CreateAssetMenu(fileName = "Battle", menuName = "TcgEngine/MapEvent/BattleFixed", order = 10)]
    public class EventBattleFixed : EventBattle
    {
        [Header("Fixed Deck")]
        public CardData[] cards;
        public CardData[] items;
        public bool dont_shuffle;

    }

}
