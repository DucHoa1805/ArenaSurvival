using System;
using ArenaSurvival.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Hub
{
    /// <summary>1 nút màn trong tab Chiến đấu: "1-2", 3 sao, khóa, viền khi đang chọn.</summary>
    public class StageNodeView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private Image[] _stars = new Image[3];
        [SerializeField] private GameObject _lock;
        [Tooltip("Màn có boss: hiện đầu lâu nhỏ.")]
        [SerializeField] private GameObject _bossMark;

        private static readonly Color FrameNormal = new Color(0.42f, 0.46f, 0.55f, 0.8f);
        private static readonly Color FrameSelected = new Color(0.95f, 0.71f, 0.27f, 1f);
        private static readonly Color FrameChapterBoss = new Color(0.88f, 0.3f, 0.26f, 1f);
        private static readonly Color StarOn = Color.white;
        private static readonly Color StarOff = new Color(1f, 1f, 1f, 0.15f);

        public StageData Stage { get; private set; }
        public event Action<StageNodeView> OnClicked;

        private void Awake() => _button.onClick.AddListener(HandleClick);
        private void OnDestroy() => _button.onClick.RemoveListener(HandleClick);

        /// <param name="chapterBoss">Màn cuối chương có boss: viền đỏ + đầu lâu to, để người chơi thấy trước "bức tường".</param>
        public void Show(StageData stage, bool unlocked, int stars, bool selected, bool chapterBoss)
        {
            Stage = stage;
            _name.text = stage.displayName;
            _name.alpha = unlocked ? 1f : 0.4f;
            _lock.SetActive(!unlocked);
            if (_bossMark != null)
            {
                _bossMark.SetActive(stage.boss != null);
                _bossMark.transform.localScale = Vector3.one * (chapterBoss ? 1.4f : 1f);
            }
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(unlocked);
                _stars[i].color = i < stars ? StarOn : StarOff;
            }
            _frame.color = selected ? FrameSelected : chapterBoss ? FrameChapterBoss : FrameNormal;
        }

        private void HandleClick() => OnClicked?.Invoke(this);
    }
}
