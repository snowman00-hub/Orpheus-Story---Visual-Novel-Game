using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 대사창, 화자 이름, 선택지 버튼을 화면에 표시한다.
public class DialogueView : MonoBehaviour
{
    private const string NarrationSpeakerName = "내레이션";

    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Transform choiceRoot;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private DialogueSpeakerStyleLibrary speakerStyles;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();

    // 대사 한 줄의 화자와 본문을 UI에 표시한다.
    public void ShowLine(DialogueLine line)
    {
        bool isNarration = line.Speaker == NarrationSpeakerName;

        speakerText.gameObject.SetActive(!isNarration);
        if (!isNarration)
        {
            speakerText.SetText(line.Speaker);
            speakerText.color = speakerStyles.GetColor(line.Speaker);
        }

        bodyText.SetText(isNarration ? line.Text : $"\"{line.Text}\"");

        ClearChoices();
    }

    // 선택지 버튼들을 생성하고 선택 콜백을 연결한다.
    public void ShowChoices(IReadOnlyList<DialogueChoiceOption> options, Action<DialogueChoiceOption> onSelected)
    {
        ClearChoices();

        foreach (DialogueChoiceOption option in options)
        {
            Button button = Instantiate(choiceButtonPrefab, choiceRoot);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.SetText(option.Label);

            DialogueChoiceOption capturedOption = option;
            button.onClick.AddListener(() => onSelected(capturedOption));
            spawnedChoiceButtons.Add(button);
        }
    }

    // 현재 표시 중인 선택지 버튼들을 모두 제거한다.
    private void ClearChoices()
    {
        foreach (Button button in spawnedChoiceButtons)
        {
            Destroy(button.gameObject);
        }

        spawnedChoiceButtons.Clear();
    }
}
