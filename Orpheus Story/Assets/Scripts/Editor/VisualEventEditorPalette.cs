using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Orpheus Story/Visual Novel/Visual Event Editor Palette")]
// VisualEvent 편집기에서 사용할 배경, 캐릭터, CG 후보 이미지를 담는다.
public class VisualEventEditorPalette : ScriptableObject
{
    [SerializeField] private List<Sprite> backgrounds = new List<Sprite>();
    [SerializeField] private List<Sprite> characters = new List<Sprite>();
    [SerializeField] private List<Sprite> cgs = new List<Sprite>();

    public IReadOnlyList<Sprite> Backgrounds => backgrounds;
    public IReadOnlyList<Sprite> Characters => characters;
    public IReadOnlyList<Sprite> Cgs => cgs;
}
