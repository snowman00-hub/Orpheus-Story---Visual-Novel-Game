using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 대사창, 화자 이름, 선택지 버튼을 화면에 표시한다.
public class DialogueView : MonoBehaviour
{
    private const string NarrationSpeakerName = "내레이션";

    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TypewriterText bodyTypewriter;
    [SerializeField] private Transform choiceRoot;
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private DialogueSpeakerStyleLibrary speakerStyles;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();
    private CancellationTokenSource typingCancellation;

    public bool IsTyping => bodyTypewriter.IsTyping;

    // 대사 한 줄의 화자와 본문을 UI에 표시한다.
    public void ShowLine(DialogueLine line)
    {
        StopTyping();
        ClearChoices();
        ApplySpeaker(line);

        typingCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PlayLineAsync(BuildBodyText(line), typingCancellation).Forget();
    }

    // 대사 한 줄의 화자와 본문을 타이핑 없이 즉시 표시한다.
    public void ShowLineImmediately(DialogueLine line)
    {
        StopTyping();
        ClearChoices();
        ApplySpeaker(line);
        bodyTypewriter.SetImmediately(BuildBodyText(line));
    }

    // 현재 타이핑 중인 문장을 즉시 끝까지 표시한다.
    public void CompleteTyping()
    {
        bodyTypewriter.CompleteImmediately();
    }

    // 선택지 이벤트를 표시하기 전에 대사 텍스트를 비운다.
    public void ShowChoiceEvent()
    {
        StopTyping();
        ClearChoices();
        speakerText.gameObject.SetActive(false);
        bodyTypewriter.SetImmediately(string.Empty);
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

    // 화자 텍스트 표시 여부와 색상을 적용한다.
    private void ApplySpeaker(DialogueLine line)
    {
        bool isNarration = line.Speaker == NarrationSpeakerName;

        speakerText.gameObject.SetActive(!isNarration);
        if (!isNarration)
        {
            speakerText.SetText(line.Speaker);
            speakerText.color = speakerStyles.GetColor(line.Speaker);
        }
    }

    // 내레이션과 일반 대사의 본문 표시 형식을 만든다.
    private static string BuildBodyText(DialogueLine line)
    {
        return line.Speaker == NarrationSpeakerName ? line.Text : $"\"{line.Text}\"";
    }

    // 본문 타이핑을 재생하고 완료된 토큰을 정리한다.
    private async UniTaskVoid PlayLineAsync(string body, CancellationTokenSource cancellationSource)
    {
        await bodyTypewriter.PlayAsync(body, cancellationSource.Token);

        if (typingCancellation != cancellationSource)
        {
            return;
        }

        typingCancellation.Dispose();
        typingCancellation = null;
    }

    // 이전 문장의 타이핑 작업을 중단한다.
    private void StopTyping()
    {
        if (typingCancellation == null)
        {
            return;
        }

        typingCancellation.Cancel();
        typingCancellation.Dispose();
        typingCancellation = null;
    }

    private void OnDestroy()
    {
        StopTyping();
    }
}
