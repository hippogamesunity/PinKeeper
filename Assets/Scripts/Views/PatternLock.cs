using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Common;
using UnityEngine;

namespace Assets.Scripts.Views
{
    public class PatternLock : LockBase
    {
        public Transform Grid;

        private const float Delta = 40;
        private static readonly List<Vector2> Digits = new List<Vector2>();
        private static readonly List<int> Input = new List<int>();
        private static readonly List<UISprite> Polyline = new List<UISprite>();
        private static readonly List<UISprite> Cells = new List<UISprite>();
        private static readonly List<UISprite> Circles = new List<UISprite>();
        private const float TweenTime = 0.25f;
        private static bool _hidden;

        public new void Start()
        {
            base.Start();
            CreateGrid();
        }

        public void Update()
        {
            if (Profile.Instance.LockTime != null) return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                CreateKey();
            }
            else if (UnityEngine.Input.GetMouseButton(0) && Input.Count > 0)
            {
                UpdateKey();
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                ProcessKey();
            }
        }

        public void HidePattern()
        {
            _hidden = !_hidden;
        }

        private void CreateGrid()
        {
            const float step = 3 * Delta;

            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    var position = new Vector2(-1.5f * step + j * step, 1.5f * step - i * step);

                    Digits.Add(position);
                    DrawCell(position, Colors.Blue);
                    DrawCircle(position, Colors.Blue.SetAlpha(0));
                }
            }
        }

        private void CreateKey()
        {
            var mouse = ScreenManager.Instance.TargetHeight / 2 * Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition) - Grid.transform.localPosition;

            TaskScheduler.Kill(TaskId);

            for (var i = 0; i < Digits.Count; i++)
            {
                if (Vector2.Distance(Digits[i], mouse) < 1.5f * Delta)
                {
                    KeyPressed(i);
                }
            }
        }

        private void UpdateKey()
        {
            var mouse = ScreenManager.Instance.TargetHeight / 2 * Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition) - Grid.transform.localPosition;

            for (var i = 0; i < Digits.Count; i++)
            {
                if (Vector2.Distance(Digits[i], mouse) < Delta)
                {
                    if (!Input.Contains(i))
                    {
                        KeyPressed(i);
                    }
                }
            }

            if (_hidden) return;

            if (Input.Count > 0)
            {
                if (Polyline.Count == Input.Count)
                {
                    UpdatePolyline(mouse);
                }
                else
                {
                    RedrawPolyline(mouse, Colors.Blue);
                }
            }
        }

        private static void KeyPressed(int i)
        {
            Input.Add(i);

            if (_hidden) return;

            Circles[i].color = Colors.Blue.SetAlpha(0);
            TweenAlpha.Begin(Circles[i].gameObject, 0, 1);
        }

        private void ProcessKey()
        {
            if (Input.Count <= 0) return;

            var success = base.ProcessKey(new ProtectedValue(string.Join(string.Empty, Input.Select(i => i.ToString("X")).ToArray())));

            if (!_hidden)
            {

                var color = success ? Colors.Green : Colors.Red;

                foreach (var key in Input)
                {
                    TweenColor.Begin(Circles[key].gameObject, TweenTime, color);
                    TweenColor.Begin(Cells[key].gameObject, TweenTime, color);
                }

                Destroy(Polyline.Last().gameObject);
                Polyline.RemoveAt(Polyline.Count - 1);
                RedrawPolyline(color);
            }

            Input.Clear();
            enabled = false;

            if (!success)
            {
                TaskScheduler.CreateTask(() => { ClearPattern(); enabled = true; }, TaskId, 1);
            }
        }

        protected override void Refresh()
        {
            base.Refresh();
            ClearPattern();
        }

        public static ProtectedValue Pattern;

        protected override void Success(ProtectedValue pattern)
        {
            ClearPattern();
            Pattern = pattern.Copy();
            base.Success(pattern);
        }

        private static void ClearPattern()
        {
            if (_hidden) return;

            DestroyPolyline();

            foreach (var circle in Circles)
            {
                TweenAlpha.Begin(circle.gameObject, TweenTime, 0);
            }

            foreach (var circle in Cells)
            {
                TweenColor.Begin(circle.gameObject, TweenTime, Profile.Instance.LockTime == null ? Colors.Blue : Colors.Red.SetAlpha(100 / 255f));
            }
        }

        private static void DestroyPolyline()
        {
            foreach (var line in Polyline)
            {
                Destroy(line.gameObject);
            }

            Polyline.Clear();
        }

        private static void UpdatePolyline(Vector2 mouse)
        {
            var line = Polyline.Last();
            var i = Digits[Input[Input.Count - 1]];

            line.transform.localPosition = (i + mouse) / 2;
            line.transform.localScale = new Vector2(Vector2.Distance(i, mouse) / 2, 1);
            line.transform.localRotation = Quaternion.Euler(0, 0, 90 + RotationHelper.Angle(mouse - i));
        }

        private void RedrawPolyline(Vector2 mouse, Color color)
        {
            if (Polyline.Count > 0)
            {
                UpdatePolyline(Digits[Input[Input.Count - 2]]);
            }

            var line = PrefabsHelper.InstantiateLine(Grid).GetComponent<UISprite>();

            line.color = color;
            Polyline.Add(line);
            UpdatePolyline(mouse);
        }

        private static void RedrawPolyline(Color color)
        {
            foreach (var line in Polyline)
            {
                TweenColor.Begin(line.gameObject, TweenTime, color);
            }
        }

        private void DrawCell(Vector2 position, Color color)
        {
            var cell = PrefabsHelper.InstantiateCell(Grid);

            cell.transform.localPosition = position;

            var sprite = cell.GetComponent<UISprite>();

            sprite.color = color;

            Cells.Add(sprite);
        }

        private void DrawCircle(Vector2 position, Color color)
        {
            var circle = PrefabsHelper.InstantiateCircle(Grid);

            circle.transform.localPosition = position;

            var sprite = circle.GetComponent<UISprite>();

            sprite.color = color;

            Circles.Add(sprite);
        }
    }
}