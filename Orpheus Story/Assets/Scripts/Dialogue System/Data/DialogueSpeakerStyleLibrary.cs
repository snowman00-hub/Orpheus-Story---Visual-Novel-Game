using System.Collections.Generic;
using UnityEngine;

// 여러 화자 스타일을 화자 이름으로 빠르게 찾기 위한 라이브러리다.
[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Dialogue Speaker Style Library")]
public class DialogueSpeakerStyleLibrary : ScriptableObject
{
    [SerializeField] private List<DialogueSpeakerStyle> styles = new List<DialogueSpeakerStyle>();
    [SerializeField] private Color defaultSpeakerColor = Color.white;

    private Dictionary<string, Color> colorsBySpeakerName;

    // 화자 이름에 해당하는 색상을 찾고, 없으면 기본 색상을 반환한다.
    public Color GetColor(string speakerName)
    {
        EnsureCache();
        return colorsBySpeakerName.TryGetValue(speakerName, out Color color) ? color : defaultSpeakerColor;
    }

    // Inspector에 등록된 화자 스타일 목록을 검색용 사전으로 캐싱한다.
    private void EnsureCache()
    {
        if (colorsBySpeakerName != null)
        {
            return;
        }

        colorsBySpeakerName = new Dictionary<string, Color>();
        foreach (DialogueSpeakerStyle style in styles)
        {
            colorsBySpeakerName[style.SpeakerName] = style.SpeakerColor;
        }
    }
}
