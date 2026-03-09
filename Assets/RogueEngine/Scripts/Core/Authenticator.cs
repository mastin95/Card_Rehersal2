using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RogueEngine
{
    /// <summary>
    /// Local stub for Authenticator to allow Single-Player mode to compile
    /// without rewriting all UI logic that depends on UserData.
    /// </summary>
    public class Authenticator : MonoBehaviour
    {
        private static Authenticator instance;

        public string Username { get; private set; } = "Player";
        public string UserID { get; private set; } = "local_player";
        
        public UserData UserData { get; private set; }

        void Awake()
        {
            instance = this;
            UserData = new UserData();
            UserData.username = Username;
        }

        public static Authenticator Get()
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("Authenticator");
                instance = obj.AddComponent<Authenticator>();
                instance.UserData = new UserData();
                instance.UserData.username = "Player";
            }
            return instance;
        }

        public bool IsConnected() { return true; }
        public bool IsSignedIn() { return true; }
        
        public async Task<bool> RefreshLogin() { await Task.Yield(); return true; }
        public async Task<bool> Login(string user, string pass) { await Task.Yield(); return true; }
        
        public string GetError() { return ""; }
        
        public async Task LoadUserData() { await Task.Yield(); }
        public async Task SaveUserData() { await Task.Yield(); }
        
        public UserData GetUserData() { return UserData; }
        
        public void Logout() { }
    }
}
