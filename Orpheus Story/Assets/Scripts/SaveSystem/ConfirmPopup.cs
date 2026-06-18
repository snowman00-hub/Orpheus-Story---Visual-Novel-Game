using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 확인이 필요한 저장/로드 선택을 한 번 더 묻는 팝업이다.
public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private bool hideOnAwake = true;

    private Action onYes;

    private void Awake()
    {
        if (hideOnAwake)
        {
            Hide();
        }
    }

    // 메시지와 확인 동작을 받아 팝업을 연다.
    public void Show(string message, Action onYes)
    {
        this.onYes = onYes;
        messageText.SetText(message);

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(Confirm);

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(Cancel);

        popupRoot.SetActive(true);
    }

    // 팝업을 닫고 등록된 확인 동작을 비운다.
    public void Hide()
    {
        onYes = null;
        popupRoot.SetActive(false);
    }

    // 예 버튼을 눌렀을 때 등록된 동작을 실행한다.
    private void Confirm()
    {
        PlayUiClick();
        Action action = onYes;
        Hide();
        action?.Invoke();
    }

    private void Cancel()
    {
        PlayUiClick();
        Hide();
    }

    private void PlayUiClick()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayUiClick();
        }
    }
}
