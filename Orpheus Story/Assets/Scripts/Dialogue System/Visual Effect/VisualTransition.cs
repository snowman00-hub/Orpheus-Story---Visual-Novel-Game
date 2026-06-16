using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 배경이나 CG가 바뀔 때 대사 출력 전에 자동 화면 전환을 재생한다.
public class VisualTransition : MonoBehaviour
{
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Color coverColor = Color.black;
    [SerializeField] private float dialogueFadeOutDuration = 0.2f;
    [SerializeField] private float coverFadeInDuration = 0.25f;
    [SerializeField] private float revealDuration = 0.8f;
    [SerializeField] private float dialogueFadeInDuration = 0.2f;
    [SerializeField] private int curtainSegmentCount = 48;
    [SerializeField] private float waveStrength = 0.08f;
    [SerializeField] private float waveFrequency = 18f;

    // 대사를 숨기고 화면을 덮은 뒤 비주얼을 교체하고 새 화면을 드러낸다.
    public async UniTask PlayAsync(DialogueView dialogueView, Action applyVisuals, CancellationToken cancellationToken)
    {
        Canvas canvas = GetRootCanvas();
        if (canvas == null)
        {
            applyVisuals?.Invoke();
            return;
        }

        GameObject overlayObject = CreateOverlayObject(canvas);
        CanvasGroup overlayGroup = overlayObject.GetComponent<CanvasGroup>();
        WavyCurtainGraphic curtainGraphic = overlayObject.GetComponent<WavyCurtainGraphic>();

        try
        {
            dialogueView.HideDialogueTextImmediate();
            await dialogueView.FadeAsync(0f, dialogueFadeOutDuration, cancellationToken);
            await FadeCanvasGroup(overlayGroup, 0f, 1f, coverFadeInDuration, cancellationToken);

            applyVisuals?.Invoke();

            await RevealCurtain(curtainGraphic, cancellationToken);
            await dialogueView.FadeAsync(1f, dialogueFadeInDuration, cancellationToken);
        }
        finally
        {
            DestroyOverlayObject(overlayObject);
        }
    }

    // 화면 전체를 덮는 물결 경계 오버레이를 만든다.
    private GameObject CreateOverlayObject(Canvas canvas)
    {
        GameObject root = new GameObject("Visual Transition Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(WavyCurtainGraphic));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        WavyCurtainGraphic curtainGraphic = root.GetComponent<WavyCurtainGraphic>();
        curtainGraphic.raycastTarget = false;
        curtainGraphic.Configure(coverColor, curtainSegmentCount, waveStrength, waveFrequency);
        curtainGraphic.SetReveal(0f);

        return root;
    }

    // 오른쪽에서 왼쪽으로 물결치는 경계선을 이동시켜 오버레이를 걷어낸다.
    private async UniTask RevealCurtain(WavyCurtainGraphic curtainGraphic, CancellationToken cancellationToken)
    {
        float elapsed = 0f;

        while (elapsed < revealDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float progress = revealDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / revealDuration);
            curtainGraphic.SetReveal(progress);

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        curtainGraphic.SetReveal(1f);
    }

    // 시간 정지 영향을 받지 않는 시간으로 CanvasGroup을 페이드한다.
    private async UniTask FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration, CancellationToken cancellationToken)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            return;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        canvasGroup.alpha = to;
    }

    private Canvas GetRootCanvas()
    {
        if (rootCanvas != null)
        {
            return rootCanvas;
        }

        return GetComponentInParent<Canvas>();
    }

    private void DestroyOverlayObject(GameObject overlayObject)
    {
        if (overlayObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(overlayObject);
        }
        else
        {
            DestroyImmediate(overlayObject);
        }
    }
}
