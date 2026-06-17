using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// 세이브 데이터를 JSON 파일과 썸네일 파일로 저장하고 불러온다.
public static class SaveSystem
{
    private const string SaveDirectoryName = "Saves";
    private const string ThumbnailDirectoryName = "Thumbnails";
    private const string SaveFilePrefix = "save_";
    private const string SaveFileExtension = ".json";
    private const string ThumbnailFileExtension = ".jpg";

    public static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveDirectoryName);
    public static string ThumbnailDirectoryPath => Path.Combine(SaveDirectoryPath, ThumbnailDirectoryName);

    // 현재 대사 정보를 지정한 슬롯에 저장한다.
    public static void Save(int slotIndex, DialogueLine line)
    {
        Save(slotIndex, line, string.Empty);
    }

    // 현재 대사 정보와 썸네일 경로를 지정한 슬롯에 저장한다.
    public static void Save(int slotIndex, DialogueLine line, string thumbnailPath)
    {
        if (line == null)
        {
            Debug.LogWarning("Cannot save because current dialogue line is null.");
            return;
        }

        Save(slotIndex, CreateData(slotIndex, line, thumbnailPath));
    }

    // 이미 만들어진 세이브 데이터를 지정한 슬롯에 저장한다.
    public static void Save(int slotIndex, SaveData saveData)
    {
        EnsureSaveDirectory();

        saveData.SlotIndex = slotIndex;
        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        File.WriteAllText(GetSaveFilePath(slotIndex), json);
    }

    // 지정한 슬롯의 세이브 데이터를 불러온다.
    public static bool TryLoad(int slotIndex, out SaveData saveData)
    {
        saveData = null;
        string path = GetSaveFilePath(slotIndex);

        if (!File.Exists(path))
        {
            return false;
        }

        string json = File.ReadAllText(path);
        saveData = JsonConvert.DeserializeObject<SaveData>(json);
        return saveData != null;
    }

    // 지정한 슬롯의 세이브 파일이 있는지 확인한다.
    public static bool HasSave(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    // 지정한 슬롯의 세이브 파일을 제거한다.
    public static void Delete(int slotIndex)
    {
        string path = GetSaveFilePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        string thumbnailPath = GetThumbnailFilePath(slotIndex);
        if (File.Exists(thumbnailPath))
        {
            File.Delete(thumbnailPath);
        }
    }

    // 지정한 슬롯의 세이브 파일 경로를 만든다.
    public static string GetSaveFilePath(int slotIndex)
    {
        string fileName = $"{SaveFilePrefix}{slotIndex:00}{SaveFileExtension}";
        return Path.Combine(SaveDirectoryPath, fileName);
    }

    // 지정한 슬롯의 썸네일 파일 경로를 만든다.
    public static string GetThumbnailFilePath(int slotIndex)
    {
        string fileName = $"{SaveFilePrefix}{slotIndex:00}{ThumbnailFileExtension}";
        return Path.Combine(ThumbnailDirectoryPath, fileName);
    }

    // 캡처한 텍스처를 지정한 슬롯의 썸네일 파일로 저장한다.
    public static string SaveThumbnail(int slotIndex, Texture2D texture)
    {
        EnsureSaveDirectory();
        EnsureThumbnailDirectory();

        string path = GetThumbnailFilePath(slotIndex);
        File.WriteAllBytes(path, texture.EncodeToJPG(80));
        return path;
    }

    // 현재 대사 정보로 세이브 데이터를 만든다.
    private static SaveData CreateData(int slotIndex, DialogueLine line, string thumbnailPath)
    {
        return new SaveData
        {
            SlotIndex = slotIndex,
            CurrentLineId = line.Id,
            Chapter = GetChapterKey(line.Id),
            PreviewText = line.Text,
            SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ThumbnailPath = thumbnailPath
        };
    }

    // 세이브 폴더가 없으면 생성한다.
    private static void EnsureSaveDirectory()
    {
        if (!Directory.Exists(SaveDirectoryPath))
        {
            Directory.CreateDirectory(SaveDirectoryPath);
        }
    }

    // 썸네일 폴더가 없으면 생성한다.
    private static void EnsureThumbnailDirectory()
    {
        if (!Directory.Exists(ThumbnailDirectoryPath))
        {
            Directory.CreateDirectory(ThumbnailDirectoryPath);
        }
    }

    // 대사 ID에서 챕터 키를 추출한다.
    private static string GetChapterKey(string lineId)
    {
        if (string.IsNullOrEmpty(lineId))
        {
            return string.Empty;
        }

        int separatorIndex = lineId.IndexOf('_');
        return separatorIndex < 0 ? lineId : lineId.Substring(0, separatorIndex);
    }
}
