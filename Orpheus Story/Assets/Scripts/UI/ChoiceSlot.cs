using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 선택지 하나의 텍스트와 클릭 동작을 담당한다.
public class ChoiceSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button button;

    private DialogueChoiceOption option;
    private Action<DialogueChoiceOption> onSelected;

    // 선택지 데이터를 받아 UI 텍스트와 클릭 콜백을 연결한다.
    public void Setup(DialogueChoiceOption option, Action<DialogueChoiceOption> onSelected)
    {
        this.option = option;
        this.onSelected = onSelected;

        label.SetText(option.Label);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Submit);
    }

    // 현재 슬롯의 선택지를 실행한다.
    private void Submit()
    {
        onSelected?.Invoke(option);
    }
}
