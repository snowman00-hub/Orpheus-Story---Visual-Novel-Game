using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

// TextMeshProUGUI 텍스트를 한 글자씩 보여주는 타이핑 효과를 담당한다.
public class TypewriterText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float charactersPerSecond = 10f;

    private bool completeRequested;
    private int playVersion;

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
        float visibleCharacterProgress = 0f;

        while (visibleCharacterProgress < characterCount && !completeRequested && !cancellationToken.IsCancellationRequested)
        {
            visibleCharacterProgress += Time.deltaTime * Mathf.Max(1f, charactersPerSecond);
            ApplyVisibleCharacterProgress(visibleCharacterProgress, characterCount);

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
        ShowAll();
    }

    // 타이핑 효과 없이 문장을 바로 표시한다.
    public void SetImmediately(string text)
    {
        playVersion++;
        completeRequested = false;
        targetText.SetText(text);
        ShowAll();
    }

    // 현재 출력 진행도에 맞춰 완성된 글자와 진행 중인 글자를 표시한다.
    private void ApplyVisibleCharacterProgress(float visibleCharacterProgress, int characterCount)
    {
        int fullVisibleCharacters = Mathf.FloorToInt(visibleCharacterProgress);
        float currentCharacterProgress = visibleCharacterProgress - fullVisibleCharacters;

        if (fullVisibleCharacters >= characterCount)
        {
            ShowAll();
            return;
        }

        targetText.maxVisibleCharacters = fullVisibleCharacters + 1;
        targetText.ForceMeshUpdate();
        ClipCharacter(fullVisibleCharacters, currentCharacterProgress);
    }

    // 지정한 글자를 왼쪽에서 오른쪽으로 드러나도록 정점과 UV를 조정한다.
    private void ClipCharacter(int characterIndex, float visibleRatio)
    {
        TMP_TextInfo textInfo = targetText.textInfo;
        if (characterIndex < 0 || characterIndex >= textInfo.characterCount)
        {
            return;
        }

        TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];
        if (!characterInfo.isVisible)
        {
            return;
        }

        int materialIndex = characterInfo.materialReferenceIndex;
        int vertexIndex = characterInfo.vertexIndex;
        float clampedRatio = Mathf.Clamp01(visibleRatio);

        // 정점과 UV의 오른쪽 두 개를 왼쪽으로 이동하여 글자가 드러나는 효과를 만든다.
        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
        Vector4[] uvs = textInfo.meshInfo[materialIndex].uvs0;

        float leftX = vertices[vertexIndex].x;
        float rightX = vertices[vertexIndex + 2].x;
        float clippedRightX = Mathf.Lerp(leftX, rightX, clampedRatio);

        float leftU = uvs[vertexIndex].x;
        float rightU = uvs[vertexIndex + 2].x;
        float clippedRightU = Mathf.Lerp(leftU, rightU, clampedRatio);

        vertices[vertexIndex + 2].x = clippedRightX;
        vertices[vertexIndex + 3].x = clippedRightX;
        uvs[vertexIndex + 2].x = clippedRightU;
        uvs[vertexIndex + 3].x = clippedRightU;

        // 변경된 정점과 UV를 텍스트에 적용한다.
        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Uv0);
    }

    // 현재 텍스트의 모든 글자를 표시 상태로 만든다.
    private void ShowAll()
    {
        targetText.maxVisibleCharacters = int.MaxValue;
        targetText.ForceMeshUpdate();
        IsTyping = false;
    }
}
