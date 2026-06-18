using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 대사 id를 기준으로 현재 대사를 진행하고 UI, 연출, 선택지를 연결한다.
public class DialogueManager : MonoBehaviour
{
    [SerializeField] private string startId = "ch01_001";
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private VisualEventController visualController;
    [SerializeField] private VisualEventLibrary visualEvents;
    [SerializeField] private DialogueChoiceLibrary choices;
    [SerializeField] private SaveLoadWindow saveLoadWindow;
    [SerializeField] private bool advanceByConfirmInput = true; // Confirm 입력으로 타이핑 완료 또는 다음 대사 진행 여부를 결정한다.
    [SerializeField] private bool skipBySkipInput = true;
    [SerializeField] private float skipInterval = 0.08f;

    private Dictionary<string, DialogueLine> linesById;
    private DialogueLine currentLine;
    private bool waitingForChoice; // 선택지 표시 중인지 여부를 나타낸다.
    private bool isApplyingLine; // 연출 적용 중인지 여부를 나타내며, 이 동안 Confirm 입력은 무시된다.
    private GameInput gameInput;

    public bool CanSave => currentLine != null && !isApplyingLine;
    public string CurrentLineId => currentLine == null ? string.Empty : currentLine.Id;

    // 시작 전에 모든 챕터 CSV를 읽어 대사 사전을 준비한다.
    private void Awake()
    {
        linesById = DialogueCsvLoader.LoadAllChaptersFromResources();
        gameInput = new GameInput();
    }

    private void OnEnable()
    {
        gameInput.Enable();
    }

    private void OnDisable()
    {
        gameInput.Disable();
    }

    private void OnDestroy()
    {
        gameInput.Dispose();
    }

    // 게임 시작 시 시작 대사를 표시하고 입력 대기 루프를 시작한다.
    private void Start()
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
        RunDialogueAsync(cancellationToken).Forget();
        RunSkipAsync(cancellationToken).Forget();
    }

    // Confirm 입력을 기다려 타이핑 완료 또는 다음 대사 진행을 처리한다.
    private async UniTaskVoid RunDialogueAsync(CancellationToken cancellationToken)
    {
        await ShowLineAsync(GameStartState.ConsumeStartLineId(startId), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            bool canceled = await UniTask
                .WaitUntil(ShouldHandleConfirmInput, cancellationToken: cancellationToken) // Confirm 입력이 발생할 때까지 기다린다.
                .SuppressCancellationThrow(); // 취소가 발생하면 true가 반환
            
            if (canceled)
            {
                return;
            }

            // Confirm 입력이 발생했을 때 타이핑 중이면 즉시 완료하고, 그렇지 않으면 다음 대사로 진행한다.
            if (dialogueView.IsTyping)
            {
                dialogueView.CompleteTyping();
            }
            else
            {
                await AdvanceAsync(cancellationToken);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    // Skip 입력을 누르고 있는 동안 대사를 빠르게 넘긴다.
    private async UniTaskVoid RunSkipAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            bool canceled = await UniTask
                .WaitUntil(ShouldHandleSkipInput, cancellationToken: cancellationToken)
                .SuppressCancellationThrow();

            if (canceled)
            {
                return;
            }

            while (ShouldHandleSkipInput() && !cancellationToken.IsCancellationRequested)
            {
                if (dialogueView.IsTyping)
                {
                    dialogueView.CompleteTyping();
                }
                else if (!await AdvanceAsync(cancellationToken))
                {
                    break;
                }

                await UniTask.Delay(
                        Mathf.Max(1, Mathf.RoundToInt(skipInterval * 1000f)),
                        DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken)
                    .SuppressCancellationThrow();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    // 현재 Confirm 입력을 처리할 수 있는지 확인한다.
    private bool ShouldHandleConfirmInput()
    {        
        if (!advanceByConfirmInput || !CanHandleDialogueInput())
        {
            return false;
        }

        if (waitingForChoice && !dialogueView.IsTyping)
        {
            return false;
        }

        // Confirm 입력이 이번 프레임에 발생했는지 확인한다.
        return gameInput.Player.Confirm.WasPerformedThisFrame();
    }

    // 현재 대사 입력을 받을 수 있는 상태인지 확인한다.
    private bool CanHandleDialogueInput()
    {
        if (currentLine == null || isApplyingLine || saveLoadWindow.IsOpen)
        {
            return false;
        }

        if (waitingForChoice && !dialogueView.IsTyping)
        {
            return false;
        }

        return true;
    }

    // Skip 입력이 현재 눌려 있고 처리 가능한 상태인지 확인한다.
    private bool ShouldHandleSkipInput()
    {
        return skipBySkipInput && CanHandleDialogueInput() && gameInput.Player.Skip.IsPressed();
    }

    // 현재 대사의 nextId를 따라 다음 대사로 이동한다.
    public void Advance()
    {
        AdvanceAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTask<bool> AdvanceAsync(CancellationToken cancellationToken)
    {
        if (!currentLine.HasNext)
        {
            Debug.Log("Dialogue reached the end.");
            return false;
        }

        await ShowLineAsync(currentLine.NextId, cancellationToken);
        return true;
    }

    // 지정한 id의 대사를 화면에 표시하고 필요한 연출과 선택지를 적용한다.
    public void ShowLine(string id)
    {
        ShowLineAsync(id, this.GetCancellationTokenOnDestroy()).Forget();
    }

    // 현재 대사 진행 상태를 지정한 슬롯에 저장한다.
    public void SaveCurrentLine(int slotIndex)
    {
        SaveCurrentLine(slotIndex, string.Empty);
    }

    // 현재 대사 진행 상태와 썸네일 경로를 지정한 슬롯에 저장한다.
    public void SaveCurrentLine(int slotIndex, string thumbnailPath)
    {
        if (!CanSave)
        {
            Debug.LogWarning("Cannot save current dialogue state.");
            return;
        }

        SaveSystem.Save(slotIndex, currentLine, thumbnailPath);
    }

    // 지정한 슬롯의 세이브 데이터를 불러와 해당 대사로 이동한다.
    public void LoadSavedLine(int slotIndex)
    {
        if (!SaveSystem.TryLoad(slotIndex, out SaveData saveData))
        {
            Debug.LogWarning($"Save slot not found: {slotIndex}");
            return;
        }

        ShowLine(saveData.CurrentLineId);
    }

    private async UniTask ShowLineAsync(string id, CancellationToken cancellationToken)
    {
        waitingForChoice = false;

        if (!linesById.TryGetValue(id, out DialogueLine line))
        {
            Debug.LogWarning($"Dialogue id not found: {id}");
            return;
        }

        StopBgmIfChapterChanged(currentLine, line);
        currentLine = line;
        isApplyingLine = true;
        try
        {
            // 대사에 연결된 화면 연출이 있으면 적용한다.
            await ApplyVisualEventAsync(line.VisualEventKey, cancellationToken);
        }
        finally
        {
            isApplyingLine = false;
        }

        if (line.HasChoice)
        {
            dialogueView.ShowChoiceEvent();
            ShowChoice(line.ChoiceKey);
            return;
        }

        dialogueView.ShowLine(line);
    }

    // visualEventKey와 연결된 연출 SO를 찾아 화면 연출 컨트롤러에 전달한다.
    private async UniTask ApplyVisualEventAsync(string visualEventKey, CancellationToken cancellationToken)
    {
        if (visualEvents.TryGet(visualEventKey, out VisualEvent visualEvent))
        {
            await visualController.ApplyAsync(visualEvent, dialogueView, cancellationToken);
        }
    }

    // choiceKey와 연결된 선택지 묶음을 UI에 표시한다.
    private static void StopBgmIfChapterChanged(DialogueLine previousLine, DialogueLine nextLine)
    {
        if (previousLine == null || SoundManager.Instance == null)
        {
            return;
        }

        if (GetChapterKey(previousLine.Id) != GetChapterKey(nextLine.Id))
        {
            SoundManager.Instance.StopBgm();
        }
    }

    private static string GetChapterKey(string lineId)
    {
        int separatorIndex = lineId.IndexOf('_');
        return separatorIndex < 0 ? lineId : lineId.Substring(0, separatorIndex);
    }

    private void ShowChoice(string choiceKey)
    {
        waitingForChoice = true;

        if (!choices.TryGet(choiceKey, out DialogueChoiceSet choiceSet))
        {
            Debug.LogWarning($"Choice key not found: {choiceKey}");
            waitingForChoice = false;
            return;
        }

        dialogueView.ShowChoices(choiceSet.Options, option =>
        {
            waitingForChoice = false;
            ShowLineAsync(option.NextId, this.GetCancellationTokenOnDestroy()).Forget();
        });
    }
}
