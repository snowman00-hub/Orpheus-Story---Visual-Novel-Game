using System;

[Serializable]
// 선택지 버튼 하나의 표시 문구와 이동할 다음 대사 id를 담는다.
public class DialogueChoiceOption
{
    public string Label;
    public string NextId;
}
