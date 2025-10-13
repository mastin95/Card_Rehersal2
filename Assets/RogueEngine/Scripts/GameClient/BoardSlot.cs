using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using RogueEngine.Client;
using RogueEngine.UI;

namespace RogueEngine.Client
{
    /// <summary>
    /// Visual representation of a Slot.cs
    /// </summary>

    public class BoardSlot : MonoBehaviour
    {
        public int x;
        public bool enemy;

        protected SpriteRenderer glow;
        protected Collider collide;
        protected Bounds bounds;
        protected float start_alpha = 0f;
        protected float current_alpha = 0f;
        protected bool is_hover = false;

        private static List<BoardSlot> slot_list = new List<BoardSlot>();

        protected virtual void Awake()
        {
            slot_list.Add(this);
            glow = GetComponent<SpriteRenderer>();
            collide = GetComponent<Collider>();
            bounds = collide.bounds;
            start_alpha = glow.color.a;
            glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0f);
            glow.enabled = true;
        }

        protected virtual void OnDestroy()
        {
            slot_list.Remove(this);
        }

        protected virtual void Start()
        {
            if (x < Slot.x_min || x > Slot.x_max)
                Debug.LogError("Board Slot X and Y value must be within the min and max set for those values, check Slot.cs script to change those min/max.");
            if(!GetSlot().IsValid())
                Debug.LogError("Slot invalid: " + x + " " + enemy);

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

        protected virtual void Update()
        {
            if (!GameClient.Get().IsBattleReady())
                return;

            Slot slot = GetSlot();
            Battle battle = GameClient.Get().GetBattle();
            BattleCharacter active = battle.GetActiveCharacter();
            HandCard dcard = HandCard.GetDrag();
            CardData icard = dcard != null ? dcard.GetCard().CardData : null;

            float target_alpha = 0f;
            if (active != null && battle.CanControlCharacter(GameClient.Get().GetPlayerID(), active))
            {
                if (icard != null && icard.IsRequireTarget() && battle.CanPlayCard(dcard.GetCard(), slot))
                    target_alpha = 1f;
                if (icard == null && is_hover && battle.CanMoveCharacter(active, slot))
                    target_alpha = 1f;
            }

            current_alpha = Mathf.MoveTowards(current_alpha, target_alpha * start_alpha, 2f * Time.deltaTime);
            glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, current_alpha);
        }

        //When clicking on the slot
        private void OnPointerClick(PointerEventData edata)
        {
            if (BattleUI.IsOverUILayer("UI"))
                return;

            if (edata.button == PointerEventData.InputButton.Left)
            {
                Battle gdata = GameClient.Get().GetBattle();
                Player player = GameClient.Get().GetPlayer();
                Slot slot = GetSlot();

                if (gdata.selector == SelectorType.None && gdata.IsPlayerActionTurn(player.player_id))
                {
                    if (Tutorial.IsTuto() && !Tutorial.Get().CanDo(TutoEndTrigger.Move, slot))
                        return;

                    BattleCharacter character = gdata.GetActiveCharacter();
                    BattleCharacter slot_character = gdata.GetSlotCharacter(slot);
                    if (slot_character == null)
                    {
                        GameClient.Get().MoveCharacter(character, slot);
                    }
                }

                if (gdata.selector == SelectorType.SelectTarget && gdata.IsPlayerSelectorTurn(player.player_id))
                {
                    if (Tutorial.IsTuto() && !Tutorial.Get().CanDo(TutoEndTrigger.SelectTarget, slot))
                        return;

                    BattleCharacter slot_character = gdata.GetSlotCharacter(slot);
                    if (slot_character != null)
                    {
                        GameClient.Get().SelectCharacter(slot_character);
                    }
                    else
                    {
                        GameClient.Get().SelectSlot(slot);
                    }
                }
            }
        }

        private void OnPointerEnter(PointerEventData eventData)
        {
            is_hover = true;
        }

        private void OnPointerExit(PointerEventData eventData)
        {
            is_hover = false;
        }

        void OnDisable()
        {
            is_hover = false;
        }

        //Find the actual slot coordinates of this board slot
        public virtual Slot GetSlot()
        {
            return new Slot(x, enemy);
        }

        public virtual bool HasSlot(Slot slot)
        {
            Slot aslot = GetSlot();
            return aslot == slot;
        }

        public virtual bool IsInside(Vector3 wpos)
        {
            return bounds.Contains(wpos);
        }

        public virtual bool IsNearest(Vector3 pos, float range = 999f)
        {
            BoardSlot nearest = GetNearest(pos, range);
            return nearest == this;
        }

        public static BoardSlot GetNearest(Vector3 pos, float range = 999f)
        {
            BoardSlot nearest = null;
            float min_dist = range;
            foreach (BoardSlot slot in GetAll())
            {
                float dist = (slot.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = slot;
                }
            }
            return nearest;
        }

        public static BoardSlot GetMouseRaycast(float range = 999f)
        {
            Ray ray = GameCamera.GetCamera().ScreenPointToRay(MouseInput.GetMousePosition());
            foreach (BoardSlot bslot in GetAll())
            {
                if (bslot.collide != null && bslot.collide.Raycast(ray, out RaycastHit hit, range))
                    return bslot;
            }
            return null;
        }

        public static BoardSlot Get(Slot slot)
        {
            foreach (BoardSlot bslot in GetAll())
            {
                if (bslot.HasSlot(slot))
                    return bslot;
            }
            return null;
        }

        public static List<BoardSlot> GetAll()
        {
            return slot_list;
        }

    }
}