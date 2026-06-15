using System;
using System.Threading;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Effects/Screen Flash")]
// 화면 전체를 지정한 색으로 덮었다가 사라지게 하는 플래시/페이드 효과다.
public class ScreenFlashEffect : VisualEffect
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float startDelay;
    [SerializeField] private float fadeInDuration = 0.05f;
    [SerializeField] private float holdDuration = 0.05f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;

    // 설정된 딜레이 후 화면 오버레이를 생성하고 페이드인/유지/페이드아웃한다.
    public override async UniTask Play(VisualEffectContext context, CancellationToken cancellationToken)
    {
        if (context.RootCanvas == null)
        {
            return;
        }

        GameObject overlayObject = CreateOverlayObject(context.RootCanvas);
        Image overlay = overlayObject.GetComponent<Image>();

        try
        {
            if (startDelay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: cancellationToken);
            }

            await FadeOverlay(overlay, color, 0f, maxAlpha, fadeInDuration, cancellationToken);

            if (holdDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
            }

            await FadeOverlay(overlay, color, maxAlpha, 0f, fadeOutDuration, cancellationToken);
        }
        finally
        {
            DestroyOverlayObject(overlayObject);
        }
    }

    // 루트 캔버스 최상단에 화면 전체 오버레이 오브젝트를 만든다.
    private GameObject CreateOverlayObject(Canvas rootCanvas)
    {
        GameObject overlayObject = new GameObject("Screen Flash Effect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(rootCanvas.transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        Image image = overlayObject.GetComponent<Image>();
        image.raycastTarget = false;
        SetOverlayColor(image, Color.white, 0f);
        return overlayObject;
    }

    // 오버레이 알파를 from에서 to까지 변경한다.
    private async UniTask FadeOverlay(Image overlay, Color color, float fromAlpha, float toAlpha, float duration, CancellationToken cancellationToken)
    {
        if (duration <= 0f)
        {
            SetOverlayColor(overlay, color, toAlpha);
            return;
        }

        float elapsed = 0f;
        double previousTime = GetCurrentTime();
        SetOverlayColor(overlay, color, fromAlpha);
        RepaintEditorViews();

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            double currentTime = GetCurrentTime();
            elapsed += Mathf.Max(0f, (float)(currentTime - previousTime));
            previousTime = currentTime;

            float alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
            SetOverlayColor(overlay, color, alpha);
            RepaintEditorViews();
        }

        SetOverlayColor(overlay, color, toAlpha);
        RepaintEditorViews();
    }

    // 오버레이 색상과 알파를 적용한다.
    private void SetOverlayColor(Image overlay, Color color, float alpha)
    {
        color.a = alpha;
        overlay.color = color;
        overlay.enabled = alpha > 0f;
    }

    // 플레이 모드 상태에 맞게 오버레이 오브젝트를 제거한다.
    private double GetCurrentTime()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return EditorApplication.timeSinceStartup;
        }
#endif

        return Time.unscaledTimeAsDouble;
    }

    private void RepaintEditorViews()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
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
