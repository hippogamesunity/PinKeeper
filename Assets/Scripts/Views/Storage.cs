using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Common;
using UnityEngine;

namespace Assets.Scripts.Views
{
    public class Storage : ViewBase
    {
        public GameObject[] Pages;
        public SelectButton[] Paging;
        public GameObject PremiumButton;
        public GameObject RestoreButton;
        public GameObject SyncButton;
        
        private int _page;

        public void Start()
        {
            for (var i = 0; i < Paging.Length; i++)
            {
                var page = i;

                Paging[i].Selected += () => ShowPage(page);
            }
        }
        
        public override void Open(TweenDirection direction, object meta)
        {
            Initialize(Profile.Instance.GetCards((ProtectedValue) meta));
            TaskScheduler.CreateTask(() => base.Open(direction), 0.1f);
        }

        private void Initialize(ICollection<PartialCardData> cards)
        {
            Pages[0].GetComponent<UIPanel>().alpha = 1;
            Pages[0].transform.localPosition = Vector2.zero;

            for (var i = 1; i < Pages.Length; i++)
            {
                Pages[i].GetComponent<UIPanel>().alpha = 0;
                Pages[i].transform.localPosition = new Vector2(1000 * Camera.main.aspect, 0);
            }

            InitializeCards(cards);
            InitializePremium(Profile.Instance.Premium);
        }

        private void InitializeCards(ICollection<PartialCardData> cards)
        {
            TweenPanel.gameObject.SetActive(true);

            foreach (var child in Pages.SelectMany(page => page.transform.Cast<Transform>()).Where(child => !child.name.Contains("Tools")))
            {
                Destroy(child.gameObject);
            }

            #if UNITY_ANDROID

            RestoreButton.SetActive(false);

            var size = Profile.Instance.Premium ? new Vector2(5, 7) : new Vector2(1, 6);

            #endif

            #if UNITY_IPHONE

            var size = Profile.Instance.Premium ? new Vector2(5, 7) : new Vector2(1, 5);

            #endif

            var slot = 0;

            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var position = new Vector2(0, 400 - y * 120);

                    if (cards.Any(i => i.Slot.Int == slot))
                    {
                        var button = PrefabsHelper.InstantiateCard(Pages[x].transform);

                        button.transform.localPosition = position;
                        button.GetComponent<Card>().Initialize(cards.Single(i => i.Slot.Int == slot));
                    }
                    else
                    {
                        var button = PrefabsHelper.InstantiateSlot(Pages[x].transform);

                        button.transform.localPosition = position;

                        var index = slot;

                        button.GetComponentInChildren<GameButton>().Up += () => GetComponent<CardEdit>().Open(TweenDirection.Right, new ProtectedValue(index));
                    }

                    slot++;

                    if (slot >= 31)
                    {
                        break;
                    }
                }
            }

            TweenPanel.gameObject.SetActive(false);
        }

        public void InitializePremium(bool premium)
        {
            PremiumButton.SetActive(false);
            SyncButton.SetActive(false);

            //PremiumButton.SetActive(!premium);
            //SyncButton.SetActive(premium);

            #if UNITY_IPHONE
            
            RestoreButton.SetActive(!premium);

            #endif

            foreach (var p in Paging)
            {
                p.Enabled = premium;
                p.Pressed = false;
            }

            if (premium)
            {
                Paging[0].Pressed = true;
                GetComponent<Cloud>().SyncInfo.SetLocalizedText(Profile.Instance.SyncTime == null ? "%SyncReady%" : Profile.Instance.SyncTime.DateTime.ToShortDateString());
            }
        }

        public void SlideLeft()
        {
            if (_page < Pages.Length - 1 && Profile.Instance.Premium)
            {
                Paging[_page + 1].Pressed = true;
            }
        }

        public void SlideRight()
        {
            if (_page > 0 && Profile.Instance.Premium)
            {
                Paging[_page - 1].Pressed = true;
            }
        }

        public void ChangePattern()
        {
            GetComponent<PatternLock>().Open(TweenDirection.Right, new Task { Type = TaskType.ChangePattern });
        }

        private void ShowPage(int page)
        {
            if (_page == page || !Profile.Instance.Premium) return;

            var side = page > _page ? 1 : -1;
            var animationCurve = TweenPanel.GetComponent<TweenPosition>().animationCurve;

            Pages[page].transform.localPosition = new Vector2(side * 1000 * Camera.main.aspect, 0);
            TweenPosition.Begin(Pages[_page], TweenPanel.DefaultTimeout, new Vector2(-side * 1000 * Camera.main.aspect, 0)).animationCurve = animationCurve;
            TweenPosition.Begin(Pages[page], TweenPanel.DefaultTimeout, Vector2.zero).animationCurve = animationCurve;
            TweenAlpha.Begin(Pages[_page], TweenPanel.DefaultTimeout / 2, 0);
            TweenAlpha.Begin(Pages[page], TweenPanel.DefaultTimeout, 1);
            _page = page;
        }
    }
}