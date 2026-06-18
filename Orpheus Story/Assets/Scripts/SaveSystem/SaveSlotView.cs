using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 세이브 슬롯 하나의 표시와 클릭 동작을 담당한다.
public class SaveSlotView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI chapterText;
    [SerializeField] private TextMeshProUGUI previewText;
    [SerializeField] private TextMeshProUGUI savedAtText;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Button button;

    private int slotIndex;
    private Action<int> onClicked;
    private Sprite thumbnailSprite;

    // 슬롯 번호와 저장 데이터를 받아 UI를 갱신한다.
    public void Setup(int slotIndex, SaveData saveData, Action<int> onClicked)
    {
        this.slotIndex = slotIndex;
        this.onClicked = onClicked;

        slotNumberText.SetText($"SLOT {slotIndex:00}");

        if (saveData == null)
        {
            chapterText.SetText("No Data");
            previewText.SetText("Empty save slot.");
            savedAtText.SetText("----.--.-- --:--");
            ClearThumbnail();
        }
        else
        {
            chapterText.SetText(saveData.Chapter);
            previewText.SetText(saveData.PreviewText);
            savedAtText.SetText(saveData.SavedAt);
            ApplyThumbnail(saveData.ThumbnailPath);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Submit);
    }

    // 현재 슬롯 클릭을 저장창에 전달한다.
    private void Submit()
    {
        PlayUiClick();
        onClicked?.Invoke(slotIndex);
    }

    private void PlayUiClick()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayUiClick();
        }
    }

    // 저장된 썸네일 파일을 읽어 슬롯 이미지로 표시한다.
    private void ApplyThumbnail(string thumbnailPath)
    {
        ClearThumbnail();

        if (string.IsNullOrEmpty(thumbnailPath) || !File.Exists(thumbnailPath))
        {
            return;
        }

        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(thumbnailPath));

        thumbnailSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        thumbnailImage.sprite = thumbnailSprite;
        thumbnailImage.enabled = true;
    }

    // 슬롯의 썸네일 이미지를 비운다.
    private void ClearThumbnail()
    {
        if (thumbnailSprite != null)
        {
            Destroy(thumbnailSprite.texture);
            Destroy(thumbnailSprite);
        }

        thumbnailImage.sprite = null;
        thumbnailImage.enabled = false;
        thumbnailSprite = null;
    }
}
