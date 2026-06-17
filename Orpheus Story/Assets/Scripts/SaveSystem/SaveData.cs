using System;

[Serializable]
// 세이브 슬롯 하나에 저장할 대사 진행 정보를 담는다.
public class SaveData
{
    public int SlotIndex { get; set; }
    public string CurrentLineId { get; set; }
    public string Chapter { get; set; }
    public string PreviewText { get; set; }
    public string SavedAt { get; set; }
}
