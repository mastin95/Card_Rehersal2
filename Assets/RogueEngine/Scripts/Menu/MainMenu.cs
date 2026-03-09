using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RogueEngine.UI
{
    /// <summary>
    /// Main script for the main menu scene
    /// </summary>

    public class MainMenu : MonoBehaviour
    {
        public AudioClip music;
        public AudioClip ambience;

        [Header("Player UI")]
        public Text username_txt;
        public AvatarUI avatar;
        public GameObject loader;

        [Header("UI")]
        public Text version_text;

        private bool starting = false;

        private static MainMenu instance;

        void Awake()
        {
            instance = this;

            //Set default settings
            BlackPanel.Get().Show(true);
            AudioTool.Get().PlayMusic("music", music);
            AudioTool.Get().PlaySFX("ambience", ambience, 0.5f, true, true);

            username_txt.text = "";
            version_text.text = "Version " + Application.version;

            if (Authenticator.Get().IsConnected())
                AfterLogin();
            else
                RefreshLogin();
        }

        private async void RefreshLogin()
        {
            bool success = await Authenticator.Get().RefreshLogin();
            if (success)
                AfterLogin();
            else
                SceneNav.GoToLoginMenu();
        }

        private void AfterLogin()
        {
            BlackPanel.Get().Hide();
            RefreshUser();
        }

        public async void RefreshUser()
        {
            await Authenticator.Get().LoadUserData();

            username_txt.text = Authenticator.Get().Username;

            UserData udata = Authenticator.Get().GetUserData();
            if (udata == null)
                return;

            AvatarData avatard = AvatarData.Get(udata.avatar);
            avatar.SetAvatar(avatard);
        }

        void Update()
        {
            // Removed matchmaking and loading UI updates related to LobbyClient
        }

        public void CreateGame(GameType type)
        {
            string user_id = Authenticator.Get().UserID;
            string file = user_id + "_solo";
            CreateGame(type, file, GameTool.GenerateRandomID());
        }

        public void CreateGame(GameType type, string filename, string game_uid)
        {
            // Directly start the game without setting GameClient connection variables
            StartGame(); 
        }

        public void LoadGame(GameType type, string filename)
        {
            string user_id = Authenticator.Get().UserID;
            World game = World.Load(filename);
            if (game != null && game.GetPlayer(user_id) != null)
            {
                StartGame();
            }
        }

        public void StartGame()
        {
            if (!starting)
            {
                starting = true;
                StartCoroutine(FadeToGame());
            }
        }

        public void OnClickSoloNew()
        {
            CreateGame(GameType.Solo);
        }

        public void OnClickSoloLoad()
        {
            string user_id = Authenticator.Get().UserID;
            LoadGame(GameType.Solo, World.GetLastSave(user_id));
        }

        public void OnClickAvatar()
        {
            AvatarPanel.Get().Show();
        }

        public void OnClickSettings()
        {
            SettingsPanel.Get().Show();
        }

        private IEnumerator FadeToGame()
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            yield return new WaitForSeconds(1f);
            SceneNav.GoToSetup();
        }

        public void OnClickLogout()
        {
            Authenticator.Get().Logout();
            StartCoroutine(FadeLogout());
        }

        private IEnumerator FadeLogout()
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            yield return new WaitForSeconds(1f);
            SceneNav.GoToLoginMenu();
        }

        public void OnClickQuit()
        {
            Application.Quit();
        }

        public static MainMenu Get()
        {
            return instance;
        }
    }
}
