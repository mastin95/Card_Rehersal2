using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RogueEngine.Gameplay;

namespace RogueEngine
{
    /// <summary>
    /// GameManager acts as the unified facade replacing the old GameClient/GameServer architecture.
    /// It interfaces directly with the local BattleLogic for a seamless Single-Player experience.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        public BattleLogic battle_logic;
        public WorldLogic world_logic;
        public World world_data;

        public void Init()
        {
            if (world_data == null)
            {
                world_data = new World();
                battle_logic = new BattleLogic(world_data);
                world_logic = new WorldLogic(battle_logic, world_data);
                
                // --- Event Wiring (UI & FX) ---
                // Re-broadcast BattleLogic events through GameManager to the UI
                battle_logic.onCharacterDamaged += (character, damage) => { onCharacterDamaged?.Invoke(character, damage); };
                battle_logic.onCardPlayed += (card, slot) => { onCardPlayed?.Invoke(card, slot); };
                battle_logic.onAbilityStart += (ability, card) => { onAbilityStart?.Invoke(ability, card); };
                battle_logic.onAbilityTargetCharacter += (ability, card, character) => { onAbilityTargetCharacter?.Invoke(ability, card, character); };
                battle_logic.onAbilityTargetCard += (ability, card, targetCard) => { onAbilityTargetCard?.Invoke(ability, card, targetCard); };
                battle_logic.onAbilityEnd += (ability, card) => { onAbilityEnd?.Invoke(ability, card); };
                battle_logic.onCharacterMoved += (character, slot) => { onCharacterMoved?.Invoke(character, slot); };
                battle_logic.onRollValue += (value) => { onValueRolled?.Invoke(value); };

                // Add a default local player
                Player p = new Player(GetPlayerID(), "local", "Player");
                world_data.players.Add(p);
            }
        }

        // UI Events
        public UnityAction onBattleStart;
        public UnityAction onNewTurn;
        public UnityAction<string> onChatMsg; // Obsolete, but kept for compilation compatibility
        public UnityAction onConnectedGame;
        public UnityAction onGameTest;
        public UnityAction onRefreshWorld;

        // FX Events
        public UnityAction<BattleCharacter, Slot> onCharacterMoved;
        public UnityAction<BattleCharacter, int> onCharacterDamaged;
        public UnityAction<Card, Slot> onCardPlayed;
        public UnityAction<AbilityData, Card> onAbilityStart;
        public UnityAction<AbilityData, Card, BattleCharacter> onAbilityTargetCharacter;
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;
        public UnityAction<AbilityData, Card> onAbilityEnd;
        public UnityAction<int> onValueRolled;

        // Map Events
        public UnityAction<MapLocation> onMapMove;
        public UnityAction<EventData> onEventStart;
        public UnityAction<EventData, EventData> onEventChoice;
        public UnityAction<CardData> onRewardChoice;
        public UnityAction<Card> onUpgradeCard;
        public UnityAction<CardData> onBuyCard;
        public UnityAction<CardData> onBuyItem;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private static bool is_quitting = false;

        void OnApplicationQuit()
        {
            is_quitting = true;
        }

        void Update()
        {
            if (world_data == null)
                return;

            float delta = Time.deltaTime;
            world_logic?.Update(delta);
            battle_logic?.Update(delta);
        }

        public static GameManager Get()
        {
            if (is_quitting) return null;
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    instance = obj.AddComponent<GameManager>();
                }
            }
            return instance;
        }

        // --- Core Endpoint Mocks based on old GameClient ---
        public bool IsConnected() { return true; }
        public bool IsGameStarted() { return GetWorld() != null; }

        public bool IsReady()
        {
            return world_data != null;
        }

        public bool IsBattleReady()
        {
            return battle_logic != null && battle_logic.GetBattleData() != null;
        }

        public bool IsYourTurn()
        {
            // In a purely single player local game, it's always 'our' turn when the Turn logic says it's not the enemy's turn.
            if (battle_logic == null) return false;
            Player player = GetPlayer();
            if (player == null) return false;
            return battle_logic.GetBattleData().IsPlayerTurn(player.player_id);
        }

        public int GetPlayerID()
        {
            // In Single Player, the local player is usually ID 0
            return 0; 
        }

        public Player GetPlayer()
        {
            if (world_data != null)
                return world_data.GetPlayer(GetPlayerID());
            return null;
        }

        public World GetWorld()
        {
            return world_data;
        }

        public Battle GetBattle()
        {
            if (battle_logic != null)
                return battle_logic.GetBattleData();
            return null;
        }

        // --- Actions ---

        public void EndTurn()
        {
            if (battle_logic != null)
                battle_logic.EndTurn();
        }

        public void SelectCard(Card card)
        {
            // UI Selection logic
            if (battle_logic != null)
            {
                // In single player, UI state can be managed locally or directly passed to battle logic.
                // We'll flesh this out as we migrate specific UI scripts.
            }
        }

        public void SelectChoice(int index)
        {
             // To be implemented
        }

        public void CancelSelection()
        {
             // To be implemented
        }

        public void LevelUp(Champion champion)
        {
             // To be implemented
        }

        public void SendChatMsg(string msg)
        {
            // Do nothing in single player
        }

        // Utility connection mocks to let UI compile
        public ConnectSettings connect_settings = ConnectSettings.Default;
        public void Connect() { if (onConnectedGame != null) onConnectedGame.Invoke(); }
        public void ConnectToHost() { }
        public void ConnectToRelay() { }
        public void Disconnect() { }

        // Mocks for Map UI and other events
        public void MapSelectChoice(Champion champion, EventData choice) { }
        public void MapSelectChoice(int choice_index) { }
        public void MapEventContinue(Champion champion = null) { }
        public void MapSelectItemReward(Champion champion, CardData item) { }
        public void MapSelectCardReward(Champion champion, CardData card) { }
        public void MapBuyCard(Champion champion, CardData card) { }
        public void MapBuyItem(Champion champion, CardData item) { }
        public void MapTrashCard(Champion champion, ChampionCard card) { }
        public void MapUpgradeCard(Champion champion, ChampionCard card) { }
        public void UseItem(BattleCharacter character, Card item) { battle_logic?.UseItem(character, item); }
        public Champion GetChampion(string uid) { return null; }
        public void StartGame() { }
        public void StartGame(int seed) { }
        public void CreateGame(ScenarioData scenario) { Init(); world_logic.CreateGame(scenario, false, "", ""); }
        public void CreateChampion(ChampionData champion, int x) { Init(); world_logic.CreateChampion(GetPlayerID(), champion, x); }
        public void DeleteChampion(int slot) { world_data?.RemoveChampion(slot); }
        public void SelectCharacter(BattleCharacter character) { battle_logic?.SelectCharacter(character); }
        public void MoveCharacter(BattleCharacter character, Slot slot) { battle_logic?.MoveCharacter(character, slot); }
        public void SelectSlot(Slot slot) { battle_logic?.SelectSlot(slot); }
        public void PlayCard(Card card, Slot slot) { battle_logic?.PlayCard(card, slot); }
        public void MapMove(MapLocation location) { world_logic?.Move(world_data?.GetFirstChampion(GetPlayerID()), location); }
        public void Resign() { }
        public void CastAbility(Card card, AbilityData ability) { }
        public void SelectCost(int cost) { battle_logic?.SelectCost(cost); }
        public void NewScenario(ScenarioData data, string test) { Init(); world_logic.CreateGame(data, false, test, test); }
        public void StartTest(WorldState state) { Init(); world_logic.StartTest(state); }
    }
}
