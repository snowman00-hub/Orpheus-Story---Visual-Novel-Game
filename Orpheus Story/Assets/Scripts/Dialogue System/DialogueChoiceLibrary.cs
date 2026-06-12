using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Dialogue Choice Library")]
// 여러 선택지 묶음을 choiceKey로 빠르게 찾기 위한 라이브러리다.
public class DialogueChoiceLibrary : ScriptableObject
{
    [SerializeField] private List<DialogueChoiceSet> choices = new List<DialogueChoiceSet>();

    private Dictionary<string, DialogueChoiceSet> choicesByKey;

    // choiceKey에 해당하는 선택지 묶음을 찾는다.
    public bool TryGet(string choiceKey, out DialogueChoiceSet choiceSet)
    {
        EnsureCache();
        return choicesByKey.TryGetValue(choiceKey, out choiceSet);
    }

    // Inspector에 등록된 선택지 묶음을 검색용 사전으로 캐싱한다.
    private void EnsureCache()
    {
        if (choicesByKey != null)
        {
            return;
        }

        choicesByKey = new Dictionary<string, DialogueChoiceSet>();
        foreach (DialogueChoiceSet choiceSet in choices)
        {
            choicesByKey[choiceSet.ChoiceKey] = choiceSet;
        }
    }
}
