using UnityEngine;
using UnityEngine.SceneManagement;

// 타이틀과 게임 씬 사이의 이동을 담당한다.
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private SaveLoadWindow saveLoadWindow;

    // 새 게임으로 게임 씬을 시작한다.
    public void NewGame()
    {
        GameStartState.SetNewGame();
        SceneManager.LoadScene(gameSceneName);
    }

    // 이어하기 버튼에서 로드 창을 연다.
    public void ContinueGame()
    {
        saveLoadWindow.OpenLoad();
    }

    // 세이브 데이터를 시작점으로 게임 씬을 연다.
    public void LoadGameFromSave(SaveData saveData)
    {
        if (saveData == null || string.IsNullOrEmpty(saveData.CurrentLineId))
        {
            Debug.LogWarning("Cannot load because save data is empty.");
            return;
        }

        GameStartState.SetLoadGame(saveData.CurrentLineId);
        SceneManager.LoadScene(gameSceneName);
    }

    // 지정한 슬롯의 세이브 데이터를 시작점으로 게임 씬을 연다.
    public void LoadGameFromSaveSlot(int slotIndex)
    {
        if (!SaveSystem.TryLoad(slotIndex, out SaveData saveData))
        {
            Debug.LogWarning($"Save slot not found: {slotIndex}");
            return;
        }

        LoadGameFromSave(saveData);
    }

    // 타이틀 씬으로 돌아간다.
    public void LoadTitle()
    {
        GameStartState.SetNewGame();
        SceneManager.LoadScene(titleSceneName);
    }

    // 게임을 종료한다.
    public void QuitGame()
    {
        Application.Quit();
    }
}
