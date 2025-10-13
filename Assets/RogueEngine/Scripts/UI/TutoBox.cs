using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RogueEngine.Client;

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
            if (Tutorial.IsTuto())
            {
                Tutorial.Get().ShowNext();
            }

            if (TutorialMap.IsTuto())
            {
                TutorialMap.Get().ShowNext();
            }
        }
    }

}
