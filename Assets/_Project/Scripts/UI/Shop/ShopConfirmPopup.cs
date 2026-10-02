using System;
using System.Collections.Generic;
using System.Text;
using ArenaSurvival.Items;
using ArenaSurvival.Loot;
using ArenaSurvival.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaSurvival.UI.Shop
{
    /// <summary>Popup xác nhận mua bằng kim cương (chống bấm nhầm trên điện thoại).</summary>
    public class ShopConfirmPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _okButton;
        [SerializeField] private Button _cancelButton;
        private Action _onOk;

        private void Awake()
        {
            _okButton.onClick.AddListener(() => { gameObject.SetActive(false); _onOk?.Invoke(); });
            _cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Show(string message, Action onOk)
        {
            _message.text = message;
            _onOk = onOk;
            gameObject.SetActive(true);
        }
    }
}
