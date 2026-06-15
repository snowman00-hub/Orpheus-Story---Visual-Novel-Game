using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 에디터 미리보기 화면에서 VisualEvent 배치 상태를 적용하고 저장용 데이터로 캡처한다.
public class VisualEventPreviewController : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cgImage;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private CharacterSlotView characterViewPrefab;

    private readonly List<CharacterSlotView> characterViews = new List<CharacterSlotView>();

    public Sprite PreviewBackground => backgroundImage.sprite;
    public Sprite PreviewCg => cgImage.sprite;
    public IReadOnlyList<CharacterSlotView> CharacterViews => characterViews;

    // 오디오 없이 VisualEvent의 화면 요소만 미리보기 화면에 적용한다.
    public void ApplyPreview(VisualEvent visualEvent)
    {
        if (visualEvent == null)
        {
            ClearPreview();
            return;
        }

        ApplyImage(backgroundImage, visualEvent.Background, visualEvent.Background != null);
        ApplyImage(cgImage, visualEvent.Cg, visualEvent.Cg != null);
        ApplyCharacters(visualEvent);
    }

    // 배경, CG, 캐릭터 슬롯을 모두 비운다.
    public void ClearPreview()
    {
        ApplyImage(backgroundImage, null, false);
        ApplyImage(cgImage, null, false);
        ClearPreviewCharacters();
    }

    // 미리보기 배경 이미지를 교체한다.
    public void SetPreviewBackground(Sprite sprite)
    {
        ApplyImage(backgroundImage, sprite, sprite != null);
    }

    // 미리보기 CG 이미지를 교체한다.
    public void SetPreviewCg(Sprite sprite)
    {
        ApplyImage(cgImage, sprite, sprite != null);
    }

    // 팔레트에서 고른 캐릭터 이미지를 새 슬롯으로 추가한다.
    public void AddPreviewCharacter(Sprite sprite)
    {
        CharacterSlotView view = CreateCharacterView();
        view.Apply(sprite, Vector2.zero, 1f, true);
    }

    // 미리보기 화면의 모든 캐릭터 슬롯 오브젝트를 제거한다.
    public void ClearPreviewCharacters()
    {
        DestroyTrackedCharacterViews();
        DestroyUntrackedCharacterViews();
        characterViews.Clear();
    }

    // 현재 미리보기 슬롯 상태를 VisualEvent 저장용 배치 목록으로 변환한다.
    public List<VisualCharacterPlacement> CapturePreviewCharacters()
    {
        var placements = new List<VisualCharacterPlacement>();

        foreach (CharacterSlotView view in characterViews)
        {
            if (view == null || view.Sprite == null || !view.Visible)
            {
                continue;
            }

            placements.Add(new VisualCharacterPlacement
            {
                Image = view.Sprite,
                AnchoredPosition = view.AnchoredPosition,
                Scale = view.Scale,
                Visible = view.Visible
            });
        }

        return placements;
    }

    // 지정한 Image 컴포넌트에 스프라이트와 표시 여부를 적용한다.
    private static void ApplyImage(Image image, Sprite sprite, bool visible)
    {
        image.sprite = sprite;
        image.enabled = visible;
    }

    // VisualEvent에 설정된 캐릭터 배치를 미리보기 슬롯에 적용한다.
    private void ApplyCharacters(VisualEvent visualEvent)
    {
        ClearPreviewCharacters();

        foreach (VisualCharacterPlacement placement in visualEvent.Characters)
        {
            CharacterSlotView view = CreateCharacterView();
            view.Apply(placement.Image, placement.AnchoredPosition, placement.Scale, placement.Visible);
        }
    }

    // 캐릭터 슬롯 프리팹을 하나 만들고 추적 목록에 추가한다.
    private CharacterSlotView CreateCharacterView()
    {
        CharacterSlotView view = Instantiate(characterViewPrefab, characterRoot);
        characterViews.Add(view);
        return view;
    }

    // 추적 중인 캐릭터 슬롯 오브젝트를 제거한다.
    private void DestroyTrackedCharacterViews()
    {
        foreach (CharacterSlotView view in characterViews)
        {
            DestroyCharacterView(view);
        }
    }

    // 도메인 리로드 등으로 추적 목록에서 빠진 미리보기 슬롯도 정리한다.
    private void DestroyUntrackedCharacterViews()
    {
        CharacterSlotView[] views = characterRoot.GetComponentsInChildren<CharacterSlotView>(true);
        foreach (CharacterSlotView view in views)
        {
            if (view == characterViewPrefab)
            {
                continue;
            }

            DestroyCharacterView(view);
        }
    }

    // 플레이 모드와 에디터 모드에 맞게 캐릭터 슬롯 오브젝트를 제거한다.
    private static void DestroyCharacterView(CharacterSlotView view)
    {
        if (view == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(view.gameObject);
        }
        else
        {
            DestroyImmediate(view.gameObject);
        }
    }
}
