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
    [SerializeField] private bool advanceByConfirmInput = true; // Confirm 입력으로 타이핑 완료 또는 다음 대사 진행 여부를 결정한다.

    private Dictionary<string, DialogueLine> linesById;
    private DialogueLine currentLine;
    private bool waitingForChoice; // 선택지 표시 중인지 여부를 나타낸다.
    private GameInput gameInput;

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
        RunDialogueAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // Confirm 입력을 기다려 타이핑 완료 또는 다음 대사 진행을 처리한다.
    private async UniTaskVoid RunDialogueAsync(CancellationToken cancellationToken)
    {
        ShowLine(startId);

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
                Advance();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    // 현재 Confirm 입력을 처리할 수 있는지 확인한다.
    private bool ShouldHandleConfirmInput()
    {        
        if (!advanceByConfirmInput || currentLine == null)
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

    // 현재 대사의 nextId를 따라 다음 대사로 이동한다.
    public void Advance()
    {
        if (!currentLine.HasNext)
        {
            Debug.Log("Dialogue reached the end.");
            return;
        }

        ShowLine(currentLine.NextId);
    }

    // 지정한 id의 대사를 화면에 표시하고 필요한 연출과 선택지를 적용한다.
    public void ShowLine(string id)
    {
        waitingForChoice = false;

        if (!linesById.TryGetValue(id, out DialogueLine line))
        {
            Debug.LogWarning($"Dialogue id not found: {id}");
            return;
        }

        currentLine = line;
        ApplyVisualEvent(line.VisualEventKey);

        if (line.HasChoice)
        {
            dialogueView.ShowChoiceEvent();
            ShowChoice(line.ChoiceKey);
            return;
        }

        dialogueView.ShowLine(line);
    }

    // visualEventKey와 연결된 연출 SO를 찾아 화면 연출 컨트롤러에 전달한다.
    private void ApplyVisualEvent(string visualEventKey)
    {
        if (visualEvents.TryGet(visualEventKey, out VisualEvent visualEvent))
        {
            visualController.Apply(visualEvent);
        }
    }

    // choiceKey와 연결된 선택지 묶음을 UI에 표시한다.
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
            ShowLine(option.NextId);
        });
    }
}
