using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 게임 실행 중 VisualEvent를 실제 화면과 오디오에 적용한다.
public class VisualEventController : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cgImage;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private CharacterSlotView characterViewPrefab;
    [SerializeField] private float characterFadeInDuration = 0.35f;

    private readonly List<CharacterSlotView> characterViews = new List<CharacterSlotView>();

    private void Awake()
    {
        ClearExistingCharacterViews();
        ApplyImage(backgroundImage, null, false);
        ApplyImage(cgImage, null, false);
    }

    // 전달받은 VisualEvent의 모든 연출 요소를 게임 화면에 반영한다.
    public void Apply(VisualEvent visualEvent)
    {
        ApplyImage(backgroundImage, visualEvent.Background, visualEvent.Background != null);
        ApplyImage(cgImage, visualEvent.Cg, visualEvent.Cg != null);
        ApplyAudio(visualEvent);
        ApplyCharacters(visualEvent);
    }

    // 지정한 Image 컴포넌트에 스프라이트와 표시 여부를 적용한다.
    private static void ApplyImage(Image image, Sprite sprite, bool visible)
    {
        image.sprite = sprite;
        image.enabled = visible;
    }

    // VisualEvent에 설정된 BGM과 효과음을 재생한다.
    private static void ApplyAudio(VisualEvent visualEvent)
    {
        if (SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlayBgm(visualEvent.Bgm);
        SoundManager.Instance.PlaySfx(visualEvent.Sfx);
    }

    // VisualEvent에 설정된 캐릭터 배치를 화면 슬롯에 적용한다.
    private void ApplyCharacters(VisualEvent visualEvent)
    {
        EnsureCharacterViewCount(visualEvent.Characters.Count);

        for (int i = 0; i < characterViews.Count; i++)
        {
            if (i < visualEvent.Characters.Count)
            {
                VisualCharacterPlacement placement = visualEvent.Characters[i];
                characterViews[i].ApplyRuntime(placement.Image, placement.AnchoredPosition, placement.Scale, placement.Visible, characterFadeInDuration);
            }
            else
            {
                characterViews[i].Hide();
            }
        }
    }

    // 필요한 캐릭터 슬롯 개수만큼 프리팹을 생성한다.
    private void EnsureCharacterViewCount(int count)
    {
        while (characterViews.Count < count)
        {
            CharacterSlotView view = Instantiate(characterViewPrefab, characterRoot);
            view.Hide();
            characterViews.Add(view);
        }
    }

    // 에디터 미리보기 후 characterRoot에 남은 캐릭터 슬롯을 플레이 시작 전에 제거한다.
    private void ClearExistingCharacterViews()
    {
        for (int i = characterRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(characterRoot.GetChild(i).gameObject);
        }

        characterViews.Clear();
    }
}
