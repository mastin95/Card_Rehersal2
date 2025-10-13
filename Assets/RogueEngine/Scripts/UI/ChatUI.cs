using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using RogueEngine.Client;

namespace RogueEngine.UI
{

    public class ChatUI : MonoBehaviour
    {
        public UIPanel send_group;
        public UIPanel msg_group;
        public InputField chat_input;
        public Key chat_key = Key.Enter;
        public Color user_color;
        public Text[] chat_lines;

        private float chat_timer = 0f;

        private List<ChatItem> chats = new List<ChatItem>();

        private static ChatUI instance;

        private void Awake()
        {
            instance = this;
        }

        void Start()
        {
            GameClient.Get().onChatMsg += OnChat;
        }

        void OnDestroy()
        {
            GameClient.Get().onChatMsg -= OnChat;
        }

        private void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (KeyInput.IsKeyPress(chat_key))
                OnPressChat();

            if (KeyInput.IsKeyPress(Key.Escape))
                CloseChat();

            chat_timer -= Time.deltaTime;
            if (msg_group.IsVisible() && chat_timer < 0f)
                msg_group.Hide();
        }

        private void RefreshUI()
        {
            foreach (Text txt in chat_lines)
                txt.text = "";

            int index = 0;
            foreach (ChatItem chat in chats)
            {
                if (index < chat_lines.Length)
                {
                    Text line = chat_lines[index];
                    if(!string.IsNullOrEmpty(chat.user))
                        line.text = "<color=#" + ColorUtility.ToHtmlStringRGB(user_color) + ">" + chat.user + ":</color> " + chat.msg;
                    else
                        line.text = chat.msg;
                    index++;
                }
            }

            chat_timer = 10f;
            msg_group.Show();
        }

        private void OnChat(string username, string msg)
        {
            ChatItem item = new ChatItem();
            item.user = username;
            item.msg = msg;
            chats.Insert(0, item);

            if (chats.Count > chat_lines.Length)
                chats.RemoveAt(chats.Count - 1);

            RefreshUI();
        }

        private void OnServerMsg(string msg)
        {
            ChatItem item = new ChatItem();
            item.user = "";
            item.msg = msg;
            chats.Insert(0, item);

            if (chats.Count > chat_lines.Length)
                chats.RemoveAt(chats.Count - 1);

            RefreshUI();
        }

        public void SendChat(string msg)
        {
            if (msg.Length > 0)
                GameClient.Get().SendChatMsg(msg);
        }

        public void OnPressChat()
        {
            if (!chat_input.isFocused)
            {
                chat_input.Select();
                chat_input.ActivateInputField();
            }

            if (send_group.IsVisible())
            {
                string msg = chat_input.text;
                chat_input.text = "";
                send_group.Hide();
                SendChat(msg);
            }
            else
            {
                chat_input.text = "";
                send_group.Show(true);
                chat_input.Select();
                chat_input.ActivateInputField();
            }
        }

        public void CloseChat()
        {
            send_group.Hide();
        }

        public bool IsOpened()
        {
            return send_group.IsVisible();
        }

        public static bool IsAnyOpened()
        {
            if (instance != null)
                return instance.IsOpened();
            return false;
        }

        public static ChatUI Get()
        {
            return instance;
        }
    }

    public class ChatItem
    {
        public string user;
        public string msg;
    }
}
