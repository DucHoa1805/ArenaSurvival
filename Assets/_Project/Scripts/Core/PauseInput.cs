using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvival.Core
{
    /// <summary>
    /// Nối phím Pause (Input System) với GameManager. Tách riêng để GameManager không phụ thuộc
    /// vào Input System, và để đổi phím chỉ cần sửa trong file .inputactions.
    /// </summary>
    public class PauseInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference _pauseAction;

        private void OnEnable()
        {
            _pauseAction.action.performed += HandlePausePerformed;
            _pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            _pauseAction.action.performed -= HandlePausePerformed;
            _pauseAction.action.Disable();
        }

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            GameManager.Instance.TogglePause();
        }
    }
}
