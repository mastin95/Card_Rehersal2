using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace RogueEngine.UI
{
    public class TutoBox : MonoBehaviour
    {
        [Header("UI")]
        public Button next_btn;

        void Awake()
        {

        }

        public void SetNextButton(bool active)
        {
            next_btn.gameObject.SetActive(active);
        }

        public void OnClickNext()
        {
            // Tutorial logic removed for single player/refactoring
        }
    }

}
