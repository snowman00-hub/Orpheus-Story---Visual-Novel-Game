using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 씬에 배치되는 캐릭터 이미지 슬롯의 표시 상태를 제어한다.
public class CharacterSlotView : MonoBehaviour
{
    [SerializeField] private Image image;

    private CancellationTokenSource fadeCancellation;

    public Sprite Sprite => image.sprite;
    public Vector2 AnchoredPosition => image.rectTransform.anchoredPosition;
    public float Scale => image.rectTransform.localScale.x;
    public bool Visible => image.enabled;

    // 에디터 미리보기처럼 연출 없이 이미지, 위치, 크기를 즉시 적용한다.
    public void ApplyInstant(Sprite sprite, Vector2 anchoredPosition, float scale, bool visible)
    {
        CancelFade();
        SetTransform(anchoredPosition, scale);
        SetSprite(sprite);
        SetAlpha(1f);
        image.enabled = visible && sprite != null;
    }

    // 런타임에서 새로 등장하는 캐릭터만 페이드인하고 기존 슬롯은 즉시 교체한다.
    public void ApplyRuntime(Sprite sprite, Vector2 anchoredPosition, float scale, bool visible, float fadeDuration)
    {
        bool wasVisible = image.enabled && image.sprite != null;
        bool shouldShow = visible && sprite != null;

        CancelFade();
        SetTransform(anchoredPosition, scale);

        if (!shouldShow)
        {
            Hide();
            return;
        }

        SetSprite(sprite);
        image.enabled = true;

        if (!wasVisible && fadeDuration > 0f)
        {
            SetAlpha(0f);
            fadeCancellation = new CancellationTokenSource();
            FadeInAsync(fadeDuration, fadeCancellation.Token).Forget();
        }
        else
        {
            SetAlpha(1f);
        }
    }

    // 기존 호출을 위해 즉시 적용으로 연결한다.
    public void Apply(Sprite sprite, Vector2 anchoredPosition, float scale, bool visible)
    {
        ApplyInstant(sprite, anchoredPosition, scale, visible);
    }

    // 캐릭터 슬롯을 화면에서 숨긴다.
    public void Hide()
    {
        CancelFade();
        SetAlpha(1f);
        image.enabled = false;
    }

    private void OnDestroy()
    {
        CancelFade();
    }

    // 이미지 슬롯의 위치와 크기를 적용한다.
    private void SetTransform(Vector2 anchoredPosition, float scale)
    {
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    // 다른 이미지일 때만 스프라이트를 교체한다.
    private void SetSprite(Sprite sprite)
    {
        if (image.sprite != sprite)
        {
            image.sprite = sprite;
        }
    }

    // 이미지 색상의 알파값만 변경한다.
    private void SetAlpha(float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    // 진행 중인 페이드 작업을 취소한다.
    private void CancelFade()
    {
        if (fadeCancellation == null)
        {
            return;
        }

        fadeCancellation.Cancel();
        fadeCancellation.Dispose();
        fadeCancellation = null;
    }

    // 알파값을 0에서 1까지 올려 캐릭터를 등장시킨다.
    private async UniTaskVoid FadeInAsync(float duration, CancellationToken cancellationToken)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        SetAlpha(1f);

        if (fadeCancellation != null && !fadeCancellation.IsCancellationRequested)
        {
            fadeCancellation.Dispose();
            fadeCancellation = null;
        }
    }
}
