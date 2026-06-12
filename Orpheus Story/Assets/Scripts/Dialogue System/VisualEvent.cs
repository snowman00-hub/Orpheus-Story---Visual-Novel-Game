using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Visual Event")]
// 대사 한 줄의 visualEventKey에 연결될 배경, CG, 캐릭터, 오디오 연출을 담는다.
public class VisualEvent : ScriptableObject
{
    [SerializeField] private string key;
    [SerializeField] private Sprite background;
    [SerializeField] private Sprite cg;
    [SerializeField] private AudioClip bgm;
    [SerializeField] private AudioClip sfx;
    [SerializeField] private List<VisualCharacterPlacement> characters = new List<VisualCharacterPlacement>();

    public string Key => key;
    public Sprite Background => background;
    public Sprite Cg => cg;
    public AudioClip Bgm => bgm;
    public AudioClip Sfx => sfx;
    public IReadOnlyList<VisualCharacterPlacement> Characters => characters;
}
