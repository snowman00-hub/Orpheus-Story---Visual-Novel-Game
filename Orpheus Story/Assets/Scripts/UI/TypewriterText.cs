using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

// TextMeshProUGUI 텍스트를 한 글자씩 보여주는 타이핑 효과를 담당한다.
public class TypewriterText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float charactersPerSecond = 10f;

    private bool completeRequested; // 현재 타이핑을 즉시 완료하도록 요청되었는지 여부를 나타낸다.
    private int playVersion; // 비동기 꼬임 방지

    public bool IsTyping { get; private set; }

    // 지정한 문장을 처음부터 한 글자씩 출력한다.
    public async UniTask PlayAsync(string text, CancellationToken cancellationToken)
    {
        int version = ++playVersion;
        completeRequested = false;
        IsTyping = true;

        targetText.SetText(text);
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();

        int characterCount = targetText.textInfo.characterCount;
        int visibleCharacters = 0;
        float secondsPerCharacter = 1f / Mathf.Max(1f, charactersPerSecond);
        float elapsedSeconds = 0f;

        while (visibleCharacters < characterCount && !completeRequested && !cancellationToken.IsCancellationRequested)
        {
            elapsedSeconds += Time.deltaTime;

            while (elapsedSeconds >= secondsPerCharacter && visibleCharacters < characterCount)
            {
                elapsedSeconds -= secondsPerCharacter;
                visibleCharacters++;
                targetText.maxVisibleCharacters = visibleCharacters;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();
        }

        if (version != playVersion)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            IsTyping = false;
            return;
        }

        ShowAll();
    }

    // 현재 문장을 즉시 끝까지 보여주도록 요청한다.
    public void CompleteImmediately()
    {
        completeRequested = true;
        targetText.maxVisibleCharacters = int.MaxValue;
    }

    // 타이핑 효과 없이 문장을 바로 표시한다.
    public void SetImmediately(string text)
    {
        playVersion++;
        completeRequested = false;
        targetText.SetText(text);
        ShowAll();
    }

    // 현재 텍스트의 모든 글자를 표시 상태로 만든다.
    private void ShowAll()
    {
        targetText.maxVisibleCharacters = int.MaxValue;
        IsTyping = false;
    }
}
