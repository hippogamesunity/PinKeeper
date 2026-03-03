using System;
using System.Linq;
using Assets.Scripts.Common;
using Assets.Scripts.Views;
using UnityEngine;

namespace Assets.Scripts
{
    public class Engine : Script
    {
        public GameObject[] Hide;
        private DateTime _pause;

        public void Start()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            DetectLanguage();
            
            #if UNITY_IPHONE

            foreach (var h in Hide) h.SetActive(true);

            #endif

            GetComponent<PatternLock>().Open(TweenDirection.Right, null);
        }

        private DateTime _down;

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }
        }

        public void OnApplicationPause(bool pause)
        {
            if (ViewBase.Current == null || ViewBase.Current is PatternLock) return;

            if (pause)
            {
                _pause = DateTime.UtcNow;
            }
            else if ((DateTime.UtcNow - _pause).TotalSeconds > 20)
            {
                GetComponent<PatternLock>().Open(TweenDirection.Left, new Task { Type = TaskType.LoadCards });
            }
        }

        public void DeleteLocalData()
        {
            if (!Profile.Instance.Premium) return;

            GetComponent<PatternLock>().Open(TweenDirection.Right, new Task { Type = TaskType.ResetProfile });
        }

        public void DeleteCloudData()
        {
            if (!Profile.Instance.Premium) return;

            GetComponent<PatternLock>().Open(TweenDirection.Right, new Task { Type = TaskType.ResetCloud });
        }

        private static void DetectLanguage()
        {
            var csv = new Func<string, byte[]>(s => Resources.Load<TextAsset>("Localization/" + s).bytes);

            Localization.LoadCSV(ByteHelper.Join(
                csv("LockBase"),
                csv("Storage"),
                csv("CardEditor"),
                csv("PinKeyboard"),
                csv("CardViewer")
            ));

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                case SystemLanguage.Ukrainian:
                    Localization.language = "Russian";
                    break;
                default:
                    Localization.language = "English";
                    break;
            }
        }

        public void Back()
        {
            if (Profile.Instance.LockTime != null)
            {
                Quit();

                return;
            }

            if (ViewBase.Current == null || FindObjectsOfType<TweenPosition>().Any(i => i.enabled))
            {
                return;
            }

            var passed = !(ViewBase.Current is LockBase);

            if (!passed)
            {
                var taskType = GetComponent<LockBase>().TaskType;

                passed = taskType != TaskType.CreatePattern && taskType != TaskType.ConfirmPattern && taskType != TaskType.LoadCards;
            }

            if (!passed || ViewBase.Current is Storage)
            {
                Quit();
            }
            else
            {
                GetComponent<Storage>().Open(TweenDirection.Left);
            }
        }

        private static void Quit()
        {
            Application.Quit();
        }
    }
}