// 게임 씬이 시작될 때 사용할 대사 시작점을 임시로 보관한다.
public static class GameStartState
{
    private static string pendingLineId;

    public static bool HasPendingLine => !string.IsNullOrEmpty(pendingLineId);

    // 새 게임 시작 상태로 초기화한다.
    public static void SetNewGame()
    {
        pendingLineId = string.Empty;
    }

    // 세이브 데이터를 통해 시작할 대사 id를 저장한다.
    public static void SetLoadGame(string lineId)
    {
        pendingLineId = lineId;
    }

    // 저장된 시작 대사 id를 한 번만 꺼내고 비운다.
    public static string ConsumeStartLineId(string defaultLineId)
    {
        if (string.IsNullOrEmpty(pendingLineId))
        {
            return defaultLineId;
        }

        string lineId = pendingLineId;
        pendingLineId = string.Empty;
        return lineId;
    }
}
