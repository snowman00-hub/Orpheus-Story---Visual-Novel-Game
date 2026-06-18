using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 버튼의 호버, 클릭, 비활성 연출을 담당한다.
public class TitleButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image targetImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.78f, 1f, 0.65f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
    [SerializeField] private Color normalTextColor = new Color(0.9f, 0.86f, 0.78f, 1f);
    [SerializeField] private Color hoverTextColor = new Color(0.8f, 1f, 0.62f, 1f);
    [SerializeField] private Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 0.85f);
    [SerializeField] private float hoverScale = 1.035f;
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float animationDuration = 0.08f;

    private RectTransform rectTransform;
    private bool isPointerInside;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        ApplyNormalState();
    }

    // 버튼 활성 상태가 바뀌었을 때 표시 상태를 다시 맞춘다.
    private void OnEnable()
    {
        ApplyNormalState();
    }

    // 포인터가 버튼 위에 올라왔을 때 호버 상태로 전환한다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        isPointerInside = true;
        ApplyHoverState();
        ScaleToAsync(hoverScale).Forget();
    }

    // 포인터가 버튼 밖으로 나갔을 때 기본 상태로 되돌린다.
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        isPointerInside = false;
        ApplyNormalState();
        ScaleToAsync(1f).Forget();
    }

    // 버튼을 누르는 순간 눌림 연출과 사운드를 재생한다.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayUiClick();
        }

        ScaleToAsync(pressedScale).Forget();
    }

    // 버튼에서 손을 뗐을 때 포인터 위치에 맞는 스케일로 복구한다.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        ScaleToAsync(isPointerInside ? hoverScale : 1f).Forget();
    }

    // 버튼을 비활성 표시로 바꾼다.
    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
        ApplyNormalState();
    }

    private bool IsInteractable()
    {
        return button.interactable;
    }

    private void ApplyNormalState()
    {
        if (!IsInteractable())
        {
            targetImage.color = disabledColor;
            label.color = disabledTextColor;
            rectTransform.localScale = Vector3.one;
            return;
        }

        targetImage.color = normalColor;
        label.color = normalTextColor;
    }

    private void ApplyHoverState()
    {
        targetImage.color = hoverColor;
        label.color = hoverTextColor;
    }

    private async UniTaskVoid ScaleToAsync(float targetScale)
    {
        Vector3 startScale = rectTransform.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        rectTransform.localScale = endScale;
    }
}
