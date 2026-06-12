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
    [SerializeField] private bool advanceWithMouseOrSpace = true; // 마우스 클릭이나 스페이스로 대사를 넘길 수 있게 할지 여부

    private Dictionary<string, DialogueLine> linesById;
    private DialogueLine currentLine;
    private bool waitingForChoice; // 선택지 표시 중에는 마우스/스페이스 입력으로 대사를 넘기지 않도록 하는 플래그
    private GameInput gameInput;

    // 시작 전에 모든 챕터 CSV를 읽어 대사 사전을 준비한다.
    private void Awake()
    {
        linesById = DialogueCsvLoader.LoadAllChaptersFromResources();
        gameInput = new GameInput();
    }

    private void OnEnable()
    {
        gameInput.Enable(); // 입력 시스템 활성화
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

    // 마우스 클릭이나 스페이스 입력을 UniTask로 기다려 다음 대사로 넘긴다.
    private async UniTaskVoid RunDialogueAsync(CancellationToken cancellationToken)
    {
        ShowLine(startId);

        while (!cancellationToken.IsCancellationRequested)
        {
            bool canceled = await UniTask
                .WaitUntil(ShouldAdvanceByInput, cancellationToken: cancellationToken) // 입력 대기
                .SuppressCancellationThrow(); // 취소 시 예외 대신 false 반환

            if (canceled)
            {
                return;
            }

            Advance(); 
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    // 현재 입력으로 대사를 넘길 수 있는지 확인한다.
    private bool ShouldAdvanceByInput()
    {
        if (!advanceWithMouseOrSpace || waitingForChoice || currentLine == null)
        {
            return false;
        }

        // 마우스 클릭이나 스페이스 입력이 이번 프레임에 발생했는지 체크
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
        dialogueView.ShowLine(line);
        ApplyVisualEvent(line.VisualEventKey);

        if (line.HasChoice)
        {
            ShowChoice(line.ChoiceKey);
        }

        Debug.Log($"{line.Speaker}: {line.Text}");
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
