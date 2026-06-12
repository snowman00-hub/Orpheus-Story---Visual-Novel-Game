using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Dialogue Choice Set")]
// 하나의 choiceKey에 속한 선택지 버튼 목록을 관리한다.
public class DialogueChoiceSet : ScriptableObject
{
    [SerializeField] private string choiceKey;
    [SerializeField] private List<DialogueChoiceOption> options = new List<DialogueChoiceOption>();

    public string ChoiceKey => choiceKey;
    public IReadOnlyList<DialogueChoiceOption> Options => options;
}
