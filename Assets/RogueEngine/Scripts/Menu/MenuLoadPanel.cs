using System.Collections;
using System.Collections.Generic;

namespace RogueEngine.UI
{

    public class MenuLoadPanel : UIPanel
    {
        private static MenuLoadPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        public void OnClickQuit()
        {
            GameManager.Get()?.Disconnect();
            Hide();
        }

        public static MenuLoadPanel Get()
        {
            return instance;
        }
    }
}
