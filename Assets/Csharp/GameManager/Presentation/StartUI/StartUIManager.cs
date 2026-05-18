using System;
using UnityEngine;

namespace Csharp.GameManager.Presentation.StartUI
{
    public class StartUIManager : MonoBehaviour
    {
        public GameObject StartUI;

        private void Awake()
        {
            if (StartUI == null) return;
            StartUI.SetActive(true);
        }

        public void StartGame()
        {
            StartUI.SetActive(false);
            MusicAudioManager.Instance.PlaySfx("afterclass");
        }

        public void ExitGame()
        {
            Application.Quit();
        }
    }
}