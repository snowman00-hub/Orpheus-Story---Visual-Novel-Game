using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Visual Event Library")]
// 여러 VisualEvent를 key로 빠르게 찾기 위한 라이브러리다.
public class VisualEventLibrary : ScriptableObject
{
    [SerializeField] private List<VisualEvent> events = new List<VisualEvent>();

    private Dictionary<string, VisualEvent> eventsByKey;

    // key에 해당하는 VisualEvent를 찾는다.
    public bool TryGet(string key, out VisualEvent visualEvent)
    {
        EnsureCache();
        return eventsByKey.TryGetValue(key, out visualEvent);
    }

    // Inspector에 등록된 VisualEvent 목록을 검색용 사전으로 캐싱한다.
    private void EnsureCache()
    {
        if (eventsByKey != null)
        {
            return;
        }

        eventsByKey = new Dictionary<string, VisualEvent>();
        foreach (VisualEvent visualEvent in events)
        {
            eventsByKey[visualEvent.Key] = visualEvent;
        }
    }
}
