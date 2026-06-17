using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 세이브/로드 창의 슬롯 생성, 저장, 로드 흐름을 담당한다.
public class SaveLoadWindow : MonoBehaviour
{
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private CanvasGroup windowCanvasGroup;
    [SerializeField] private bool allowSave;
    [SerializeField] private int slotCount = 8;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private SaveSlotView slotPrefab;
    [SerializeField] private Transform slotRoot;
    [SerializeField] private Button saveModeButton;
    [SerializeField] private Button loadModeButton;
    [SerializeField] private Button closeButton;

    private SaveLoadMode currentMode = SaveLoadMode.Load;

    public bool IsOpen => windowRoot.activeSelf;

    private void Awake()
    {
        BindButtons();
    }

    // 저장 모드로 세이브 창을 연다.
    public void OpenSave()
    {
        if (!allowSave)
        {
            Debug.LogWarning("Save is not allowed in this scene.");
            return;
        }

        currentMode = SaveLoadMode.Save;
        Open();
    }

    // 로드 모드로 세이브 창을 연다.
    public void OpenLoad()
    {
        currentMode = SaveLoadMode.Load;
        Open();
    }

    // 현재 모드로 창을 열고 슬롯 목록을 갱신한다.
    public void Open()
    {
        windowRoot.SetActive(true);
        RefreshSlots();
    }

    // 세이브 창을 닫는다.
    public void Close()
    {
        windowRoot.SetActive(false);
    }

    // 슬롯 UI를 현재 세이브 파일 상태에 맞게 다시 만든다.
    public void RefreshSlots()
    {
        ClearSlots();

        for (int i = 1; i <= slotCount; i++)
        {
            SaveSlotView slot = Instantiate(slotPrefab, slotRoot);
            slot.gameObject.SetActive(true);

            SaveSystem.TryLoad(i, out SaveData saveData);
            slot.Setup(i, saveData, OnSlotClicked);
        }
    }

    // 슬롯 클릭을 현재 저장/로드 모드에 맞게 처리한다.
    private void OnSlotClicked(int slotIndex)
    {
        if (currentMode == SaveLoadMode.Save)
        {
            ConfirmSave(slotIndex);
            return;
        }

        ConfirmLoad(slotIndex);
    }

    // 지정한 슬롯에 현재 대사를 저장할지 확인한다.
    private void ConfirmSave(int slotIndex)
    {
        if (!allowSave || !dialogueManager.CanSave)
        {
            Debug.LogWarning("Cannot save now.");
            return;
        }

        confirmPopup.Show($"Save to slot {slotIndex:00}?", () =>
        {
            SaveWithThumbnailAsync(slotIndex).Forget();
        });
    }

    // 세이브창을 캡처에서 제외하고 현재 화면 썸네일과 대사 진행을 저장한다.
    private async UniTaskVoid SaveWithThumbnailAsync(int slotIndex)
    {
        float previousAlpha = windowCanvasGroup.alpha;
        bool previousInteractable = windowCanvasGroup.interactable;
        bool previousBlocksRaycasts = windowCanvasGroup.blocksRaycasts;
        Texture2D screenshot = null;

        try
        {
            windowCanvasGroup.alpha = 0f;
            windowCanvasGroup.interactable = false;
            windowCanvasGroup.blocksRaycasts = false;
            confirmPopup.Hide();

            await UniTask.WaitForEndOfFrame(this, this.GetCancellationTokenOnDestroy());
            await UniTask.WaitForEndOfFrame(this, this.GetCancellationTokenOnDestroy());

            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            string thumbnailPath = SaveSystem.SaveThumbnail(slotIndex, screenshot);
            dialogueManager.SaveCurrentLine(slotIndex, thumbnailPath);
        }
        finally
        {
            windowCanvasGroup.alpha = previousAlpha;
            windowCanvasGroup.interactable = previousInteractable;
            windowCanvasGroup.blocksRaycasts = previousBlocksRaycasts;

            if (screenshot != null)
            {
                Destroy(screenshot);
            }
        }

        RefreshSlots();
    }

    // 지정한 슬롯에서 저장 데이터를 불러올지 확인한다.
    private void ConfirmLoad(int slotIndex)
    {
        if (!SaveSystem.TryLoad(slotIndex, out SaveData saveData))
        {
            return;
        }

        confirmPopup.Show($"Load slot {slotIndex:00}?", () =>
        {
            if (dialogueManager != null)
            {
                dialogueManager.LoadSavedLine(slotIndex);
                Close();
                return;
            }

            sceneLoader.LoadGameFromSave(saveData);
        });
    }

    // 슬롯 루트 아래의 기존 슬롯들을 지운다.
    private void ClearSlots()
    {
        for (int i = slotRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = slotRoot.GetChild(i).gameObject;
            if (child == slotPrefab.gameObject)
            {
                child.SetActive(false);
                continue;
            }

            Destroy(child);
        }
    }

    // 세이브/로드/닫기 버튼에 동작을 연결한다.
    private void BindButtons()
    {
        saveModeButton.onClick.RemoveAllListeners();
        saveModeButton.onClick.AddListener(OpenSave);
        saveModeButton.gameObject.SetActive(allowSave);

        loadModeButton.onClick.RemoveAllListeners();
        loadModeButton.onClick.AddListener(OpenLoad);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);
    }
}
