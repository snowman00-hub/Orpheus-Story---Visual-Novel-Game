using System;
using UnityEngine;

[Serializable]
// VisualEvent에서 보여줄 캐릭터 이미지와 이미지별 위치, 크기를 정의한다.
public class VisualCharacterPlacement
{
    public Sprite Image;
    public Vector2 AnchoredPosition;
    public float Scale = 1f;
    public bool Visible = true;
}
