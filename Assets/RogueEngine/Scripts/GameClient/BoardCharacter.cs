using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RogueEngine.UI;
using RogueEngine.Client;
using UnityEngine.Events;

namespace RogueEngine
{

    public class BoardCharacter : MonoBehaviour
    {
        public float move_speed = 5f;
        public float death_delay = 0.7f;

        public UnityAction onKill;

        private CharacterUI ui;

        private string uid;
        private string character_id;
        private bool killed = false;
        private bool focus = false;

        private Collider collide;
        private float update_timer = 0f;
        private float delayed_damage_timer = 0f;
        private int delayed_damage = 0;
        private int prev_hp = 0;

        private static List<BoardCharacter> character_list = new List<BoardCharacter>();

        void Awake()
        {
            character_list.Add(this);
            ui = GetComponentInChildren<CharacterUI>();
            collide = GetComponent<Collider>();
        }

        void OnDestroy()
        {
            character_list.Remove(this);
        }

        protected virtual void Start()
        {
            EventTrigger etrigger = GetComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnPointerClick((PointerEventData)data); });
            etrigger.triggers.Add(entry);

            EventTrigger.Entry entry2 = new EventTrigger.Entry();
            entry2.eventID = EventTriggerType.PointerEnter;
            entry2.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data); });
            etrigger.triggers.Add(entry2);

            EventTrigger.Entry entry3 = new EventTrigger.Entry();
            entry3.eventID = EventTriggerType.PointerExit;
            entry3.callback.AddListener((data) => { OnPointerExit((PointerEventData)data); });
            etrigger.triggers.Add(entry3);
        }

        void Update()
        {
            if (!GameClient.Get().IsBattleReady())
                return;

            UpdateUI();
            UpdatePosition();

            delayed_damage_timer -= Time.deltaTime;
            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = 0f;
                SlowUpdate();
            }
        }

        private void UpdateUI()
        {
            Battle battle = GameClient.Get().GetBattle();
            BattleCharacter character = battle.GetCharacter(uid);
            if (character == null)
                return;

            ui?.Set(character);
            ui?.SetHP(prev_hp);

            if (!IsDamagedDelayed())
                prev_hp = character.GetHP();
        }

        private void UpdatePosition()
        {
            Battle battle = GameClient.Get().GetBattle();
            BattleCharacter character = battle.GetCharacter(uid);

            Vector3 tpos = transform.position;
            BoardSlot bslot = BoardSlot.Get(character.slot);
            if (bslot != null)
                tpos = bslot.transform.position;

            transform.position = Vector3.MoveTowards(transform.position, tpos, move_speed * Time.deltaTime);
        }

        private void SlowUpdate()
        {
            
        }

        public void SetChampion(Champion champion)
        {
            uid = champion.uid;
            character_id = champion.character_id;
            ui?.Set(champion);
        }

        public void SetCharacter(BattleCharacter character)
        {
            uid = character.uid;
            character_id = character.character_id;
            prev_hp = character.GetHP();

            BoardSlot bslot = BoardSlot.Get(character.slot);
            if (bslot != null)
                transform.position = bslot.transform.position;
        }

        public void Kill()
        {
            killed = true;
            onKill?.Invoke();
            Destroy(gameObject, death_delay);
        }

        //Offset the HP visuals by a value so the HP dont go down before end of animation (like a projectile)
        public void DelayDamage(int damage, float duration = 1f)
        {
            if (damage != 0)
            {
                delayed_damage = damage;
                delayed_damage_timer = duration;
            }
        }

        public int GetDelayedHP()
        {
            BattleCharacter character = GetCharacter(); 
            if (delayed_damage_timer > 0f && character.IsDead())
                return delayed_damage; //Dead
            if (delayed_damage_timer > 0f)
                return character.GetHP(delayed_damage);
            return character.GetHP();
        }

        public bool IsDamagedDelayed()
        {
            return delayed_damage_timer > 0f;
        }

        public bool IsDead()
        {
            return killed;
        }

        public BattleCharacter GetCharacter()
        {
            Battle battle = GameClient.Get().GetBattle();
            BattleCharacter character = battle?.GetCharacter(uid);
            return character;
        }

        public string GetCharacterID()
        {
            return character_id;
        }

        public int GetPlayerID()
        {
            BattleCharacter character = GetCharacter();
            if(character != null)
                return character.player_id;
            return -1;
        }

        public string GetUID()
        {
            return uid;
        }

        public bool IsFocus()
        {
            return focus;
        }

        public bool IsActive()
        {
            Battle battle = GameClient.Get().GetBattle();
            BattleCharacter active = battle.GetActiveCharacter();
            return active != null && GetUID() == battle.GetActiveCharacter().uid;
        }

        private void OnPointerClick(PointerEventData edata)
        {
            if (BattleUI.IsOverUILayer("UI"))
                return;

            if (edata.button == PointerEventData.InputButton.Left)
            {
                PlayerControls controls = PlayerControls.Get();
                if (controls != null)
                {
                    controls.SelectCharacter(this);
                }
            }
        }

        private void OnPointerEnter(PointerEventData eventData)
        {
            focus = true;
        }

        private void OnPointerExit(PointerEventData eventData)
        {
            focus = false;
        }

        private void OnDisable()
        {
            focus = false;
        }

        public static BoardCharacter Get(string uid)
        {
            foreach (BoardCharacter character in character_list)
            {
                if (character.uid == uid)
                    return character;
            }
            return null;
        }

        public static BoardCharacter GetFocus()
        {
            foreach (BoardCharacter character in character_list)
            {
                if (character.focus)
                    return character;
            }
            return null;
        }

        public static BoardCharacter GetMouseRaycast(float range = 999f)
        {
            Ray ray = GameCamera.GetCamera().ScreenPointToRay(MouseInput.GetMousePosition());
            foreach (BoardCharacter bcharacter in GetAll())
            {
                if (bcharacter.collide != null && bcharacter.collide.Raycast(ray, out RaycastHit hit, range))
                    return bcharacter;
            }
            return null;
        }

        public static List<BoardCharacter> GetAll()
        {
            return character_list;
        }
    }
}