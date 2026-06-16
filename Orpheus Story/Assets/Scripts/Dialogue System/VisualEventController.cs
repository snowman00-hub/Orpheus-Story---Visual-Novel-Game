using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 게임 실행 중 VisualEvent를 실제 화면, 오디오, 효과에 적용한다.
public class VisualEventController : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cgImage;
    [SerializeField] private Canvas effectRootCanvas;
    [SerializeField] private VisualTransition visualTransition;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private CharacterSlotView characterViewPrefab;
    [SerializeField] private float characterFadeInDuration = 0.35f;
    [SerializeField] private float characterFadeOutDuration = 0.25f;

    private readonly List<CharacterSlotView> characterViews = new List<CharacterSlotView>();
    private VisualEffectContext effectContext;
    private CancellationTokenSource effectCancellation;
    private bool hasAppliedVisualEvent; // 첫 번째 VisualEvent에서는 화면 전환 연출을 건너뛰기 위한 플래그

    private void Awake()
    {
        effectContext = new VisualEffectContext(effectRootCanvas);
        ClearExistingCharacterViews();
        ApplyImage(backgroundImage, null, false);
        ApplyImage(cgImage, null, false);
    }

    private void OnDestroy()
    {
        CancelEffects();
    }

    // 전달받은 VisualEvent의 화면 상태를 적용하고, 붙어 있는 효과를 비동기로 재생한다.
    public void Apply(VisualEvent visualEvent)
    {
        CancelEffects();

        ApplyPrimaryVisuals(visualEvent);
        ApplyAudio(visualEvent);
        ApplyCharacters(visualEvent);
        PlayEffects(visualEvent);
        hasAppliedVisualEvent = true;
    }

    // 지정한 Image 컴포넌트에 스프라이트와 표시 여부를 적용한다.
    // VisualEvent를 적용하되 배경이나 CG가 바뀌면 자동 전환 연출을 기다린다.
    public async UniTask ApplyAsync(VisualEvent visualEvent, DialogueView dialogueView, CancellationToken cancellationToken)
    {
        CancelEffects();

        // 배경이나 CG가 바뀌는 경우에만 전환 연출을 적용한다.
        if (HasPrimaryVisualChanged(visualEvent) && visualTransition != null && dialogueView != null)
        {
            await visualTransition.PlayAsync(dialogueView, () => ApplyVisualsAndCharacters(visualEvent), cancellationToken);
        }
        else
        {
            ApplyVisualsAndCharacters(visualEvent);
        }

        ApplyAudio(visualEvent);
        PlayEffects(visualEvent);
        hasAppliedVisualEvent = true;
    }

    // 배경과 CG만 적용한다.
    private void ApplyPrimaryVisuals(VisualEvent visualEvent)
    {
        ApplyImage(backgroundImage, visualEvent.Background, visualEvent.Background != null);
        ApplyImage(cgImage, visualEvent.Cg, visualEvent.Cg != null);
    }

    // 현재 화면의 배경 또는 CG가 다음 VisualEvent와 다른지 확인한다.
    // 배경, CG, 캐릭터 배치를 한 번에 적용한다.
    private void ApplyVisualsAndCharacters(VisualEvent visualEvent)
    {
        ApplyPrimaryVisuals(visualEvent);
        ApplyCharacters(visualEvent);
    }
         
    private bool HasPrimaryVisualChanged(VisualEvent visualEvent)
    {
        if (!hasAppliedVisualEvent)
        {
            return false;
        }

        return backgroundImage.sprite != visualEvent.Background || cgImage.sprite != visualEvent.Cg;
    }

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
                characterViews[i].HideRuntime(characterFadeOutDuration);
            }
        }
    }

    // VisualEvent에 붙은 효과들을 순서대로 재생한다.
    private void PlayEffects(VisualEvent visualEvent)
    {
        if (visualEvent.Effects.Count == 0)
        {
            return;
        }

        effectCancellation = new CancellationTokenSource();
        PlayEffectsAsync(visualEvent.Effects, effectCancellation).Forget();
    }

    // 이전 효과가 취소되면 현재 효과 재생도 즉시 멈춘다.
    private async UniTaskVoid PlayEffectsAsync(IReadOnlyList<VisualEffect> effects, CancellationTokenSource cancellationSource)
    {
        try
        {
            foreach (VisualEffect effect in effects)
            {
                cancellationSource.Token.ThrowIfCancellationRequested();

                if (effect == null)
                {
                    continue;
                }

                await effect.Play(effectContext, cancellationSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (effectCancellation == cancellationSource)
            {
                effectCancellation = null;
            }

            cancellationSource.Dispose();
        }
    }

    // 진행 중인 VisualEvent 효과를 취소한다.
    private void CancelEffects()
    {
        if (effectCancellation == null)
        {
            return;
        }

        effectCancellation.Cancel();
        effectCancellation = null;
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

    // 에디터 미리보기 후 characterRoot에 남은 자식 오브젝트를 플레이 시작 전에 제거한다.
    private void ClearExistingCharacterViews()
    {
        for (int i = characterRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(characterRoot.GetChild(i).gameObject);
        }

        characterViews.Clear();
    }
}
