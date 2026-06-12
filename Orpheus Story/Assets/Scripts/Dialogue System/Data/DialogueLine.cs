using System;

[Serializable]
// CSV에서 읽어온 대사 한 줄의 데이터를 담는다.
public class DialogueLine
{
    public string Id { get; set; }
    public string Speaker { get; set; }
    public string Text { get; set; }
    public string VisualEventKey { get; set; }
    public string ChoiceKey { get; set; }
    public string NextId { get; set; }

    public bool HasChoice => !string.IsNullOrWhiteSpace(ChoiceKey);
    public bool HasNext => !string.IsNullOrWhiteSpace(NextId);
}
