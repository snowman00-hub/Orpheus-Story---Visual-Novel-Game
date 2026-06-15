using UnityEngine;
using UnityEngine.UI;

// 씬에 배치된 캐릭터 이미지 슬롯을 제어한다.
public class CharacterSlotView : MonoBehaviour
{
    [SerializeField] private Image image;

    public Sprite Sprite => image.sprite;
    public Vector2 AnchoredPosition => image.rectTransform.anchoredPosition;
    public float Scale => image.rectTransform.localScale.x;
    public bool Visible => image.enabled;

    // 슬롯에 표시할 이미지, 위치, 크기를 적용한다.
    public void Apply(Sprite sprite, Vector2 anchoredPosition, float scale, bool visible)
    {
        image.sprite = sprite;
        image.enabled = visible && sprite != null;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    // 캐릭터 슬롯을 화면에서 숨긴다.
    public void Hide()
    {
        image.enabled = false;
    }
}
