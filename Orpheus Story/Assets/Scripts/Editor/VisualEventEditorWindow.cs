using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 현재 씬의 UI를 사용해 VisualEvent 데이터를 빠르게 편집하는 에디터 창이다.
public class VisualEventEditorWindow : EditorWindow
{
    private const string VisualEventLibraryKey = "OrpheusStory.VisualEventEditor.VisualEventLibrary";
    private const string PaletteKey = "OrpheusStory.VisualEventEditor.Palette";
    private const string ShowReferencesKey = "OrpheusStory.VisualEventEditor.ShowReferences";
    private const string ShowDialogueInfoKey = "OrpheusStory.VisualEventEditor.ShowDialogueInfo";
    private const string ShowVisualEventInfoKey = "OrpheusStory.VisualEventEditor.ShowVisualEventInfo";
    private const float PaletteCellWidth = 132f;
    private const float PaletteImageSize = 112f;
    private const float PaletteRowHeight = 148f;
    private const int PaletteExtraRows = 2;

    private readonly string[] chapters = { "ch01", "ch02", "ch03", "ch04", "ch05", "ch06", "ch07" };
    private readonly string[] paletteTabs = { "Backgrounds", "Characters", "CGs" };

    private int chapterIndex;
    private int lineIndex;
    private int paletteTabIndex;
    private Vector2 scrollPosition;
    private bool showReferences;
    private bool showDialogueInfo;
    private bool showVisualEventInfo;
    private DialogueView dialogueView;
    private VisualEventPreviewController previewController;
    private VisualEventLibrary visualEventLibrary;
    private VisualEventEditorPalette palette;
    private List<DialogueLine> currentChapterLines = new List<DialogueLine>();
    private VisualEvent currentVisualEvent;

    [MenuItem("Tools/Orpheus Story/Visual Event Editor")]
    public static void Open()
    {
        GetWindow<VisualEventEditorWindow>("Visual Event Editor");
    }

    private DialogueLine CurrentLine => currentChapterLines.Count == 0 ? null : currentChapterLines[lineIndex];

    private void OnEnable()
    {
        RestoreFoldoutStates();
        RestoreAssetReferences();
        FindSceneReferences();
        LoadChapterLines();
        LoadCurrentVisualEvent();
    }

    private void OnGUI()
    {
        DrawOptionalSections();
        DrawDialogueNavigation();
        EditorGUILayout.Space(8f);
        DrawVisualEventControls();
        EditorGUILayout.Space(8f);
        DrawPalette();
    }

    // 평소에는 접어둘 수 있는 보조 정보를 표시한다.
    private void DrawOptionalSections()
    {
        EditorGUI.BeginChangeCheck();
        showReferences = EditorGUILayout.Foldout(showReferences, "References", true);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(ShowReferencesKey, showReferences);
        }

        if (showReferences)
        {
            DrawReferences();
            EditorGUILayout.Space(8f);
        }

        EditorGUI.BeginChangeCheck();
        showDialogueInfo = EditorGUILayout.Foldout(showDialogueInfo, "Dialogue Info", true);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(ShowDialogueInfoKey, showDialogueInfo);
        }

        if (showDialogueInfo)
        {
            DrawCurrentDialogue();
            EditorGUILayout.Space(8f);
        }
    }

    // 현재 씬에 있는 편집 대상 컴포넌트를 찾는다.
    private void FindSceneReferences()
    {
        dialogueView = FindSceneObject<DialogueView>();
        previewController = FindSceneObject<VisualEventPreviewController>();
    }

    // 현재 챕터의 대사 목록을 id 순서로 불러온다.
    private void LoadChapterLines()
    {
        string chapter = chapters[chapterIndex];
        currentChapterLines = DialogueCsvLoader
            .LoadAllChaptersFromResources()
            .Values
            .Where(line => line.Id.StartsWith(chapter + "_"))
            .OrderBy(line => line.Id)
            .ToList();

        lineIndex = Mathf.Clamp(lineIndex, 0, Mathf.Max(0, currentChapterLines.Count - 1));
    }

    // 현재 대사의 visualEventKey와 연결된 VisualEvent를 불러온다.
    private void LoadCurrentVisualEvent()
    {
        currentVisualEvent = null;

        if (visualEventLibrary == null || CurrentLine == null)
        {
            return;
        }

        visualEventLibrary.TryGet(CurrentLine.VisualEventKey, out currentVisualEvent);
    }

    // 에디터 창에서 사용할 참조를 표시한다.
    private void DrawReferences()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Find Scene References", GUILayout.Width(160f)))
            {
                FindSceneReferences();
            }
        }

        dialogueView = (DialogueView)EditorGUILayout.ObjectField("Dialogue View", dialogueView, typeof(DialogueView), true);
        previewController = (VisualEventPreviewController)EditorGUILayout.ObjectField("Preview Controller", previewController, typeof(VisualEventPreviewController), true);

        EditorGUI.BeginChangeCheck();
        visualEventLibrary = (VisualEventLibrary)EditorGUILayout.ObjectField("Visual Event Library", visualEventLibrary, typeof(VisualEventLibrary), false);
        palette = (VisualEventEditorPalette)EditorGUILayout.ObjectField("Palette", palette, typeof(VisualEventEditorPalette), false);
        if (EditorGUI.EndChangeCheck())
        {
            SaveAssetReferences();
            LoadCurrentVisualEvent();
            RefreshCurrentPreview();
        }
    }

    // 챕터와 대사를 이동하는 UI를 그린다.
    private void DrawDialogueNavigation()
    {
        EditorGUILayout.LabelField("Dialogue", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        chapterIndex = EditorGUILayout.Popup("Chapter", chapterIndex, chapters);
        if (EditorGUI.EndChangeCheck())
        {
            lineIndex = 0;
            LoadChapterLines();
            LoadCurrentVisualEvent();
            RefreshCurrentPreview();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Previous"))
            {
                MoveLine(-1);
            }

            if (GUILayout.Button("Next"))
            {
                MoveLine(1);
            }
        }

        DialogueLine line = CurrentLine;
        string lineLabel = line == null ? "No Line" : $"{line.Id}  {line.Speaker}";
        EditorGUILayout.LabelField("Line", $"{lineIndex + 1} / {currentChapterLines.Count}   {lineLabel}");
    }

    // 현재 대사의 상세 정보를 표시한다.
    private void DrawCurrentDialogue()
    {
        DialogueLine line = CurrentLine;
        if (line == null)
        {
            EditorGUILayout.HelpBox("No dialogue lines found.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("ID", line.Id);
        EditorGUILayout.LabelField("Speaker", line.Speaker);
        EditorGUILayout.LabelField("Visual Event Key", line.VisualEventKey);
        EditorGUILayout.LabelField("Choice Key", line.ChoiceKey);
        EditorGUILayout.LabelField("Text");
        EditorGUILayout.HelpBox(line.Text, MessageType.None);
    }

    // VisualEvent 관련 버튼들을 표시한다.
    private void DrawVisualEventControls()
    {
        EditorGUILayout.LabelField("Visual Event", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create/Load"))
            {
                currentVisualEvent = GetOrCreateCurrentVisualEvent();
                RefreshCurrentPreview();
            }

            if (GUILayout.Button("Save"))
            {
                SaveCurrentVisualEvent();
            }

            if (GUILayout.Button("Reset Preview"))
            {
                ResetPreview();
            }

            if (GUILayout.Button("Copy Previous"))
            {
                CopyPreviousVisualEvent();
            }
        }

        EditorGUI.BeginChangeCheck();
        showVisualEventInfo = EditorGUILayout.Foldout(showVisualEventInfo, "Current Event Info", true);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(ShowVisualEventInfoKey, showVisualEventInfo);
        }

        if (showVisualEventInfo)
        {
            currentVisualEvent = (VisualEvent)EditorGUILayout.ObjectField("Current", currentVisualEvent, typeof(VisualEvent), false);
        }
    }

    // 팔레트에 등록된 이미지를 썸네일 버튼으로 표시한다.
    private void DrawPalette()
    {
        if (palette == null)
        {
            EditorGUILayout.HelpBox("Assign a VisualEventEditorPalette asset to use image buttons.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        paletteTabIndex = GUILayout.Toolbar(paletteTabIndex, paletteTabs);
        if (EditorGUI.EndChangeCheck())
        {
            scrollPosition = Vector2.zero;
            GUI.FocusControl(null);
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawSpriteGrid(GetCurrentPaletteSprites(), GetCurrentPaletteAction());
        EditorGUILayout.EndScrollView();
    }

    // 현재 선택된 팔레트 탭의 스프라이트 목록을 반환한다.
    private IReadOnlyList<Sprite> GetCurrentPaletteSprites()
    {
        if (paletteTabIndex == 0)
        {
            return palette.Backgrounds;
        }

        if (paletteTabIndex == 1)
        {
            return palette.Characters;
        }

        return palette.Cgs;
    }

    // 현재 선택된 팔레트 탭의 클릭 동작을 반환한다.
    private System.Action<Sprite> GetCurrentPaletteAction()
    {
        if (paletteTabIndex == 0)
        {
            return SetBackground;
        }

        if (paletteTabIndex == 1)
        {
            return AddCharacter;
        }

        return SetCg;
    }

    // 스프라이트 목록을 보이는 범위만 썸네일 격자로 그린다.
    private void DrawSpriteGrid(IReadOnlyList<Sprite> sprites, System.Action<Sprite> onSelected)
    {
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / PaletteCellWidth));
        int rowCount = Mathf.CeilToInt((float)sprites.Count / columns);
        int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(scrollPosition.y / PaletteRowHeight) - PaletteExtraRows);
        int visibleRows = Mathf.CeilToInt(position.height / PaletteRowHeight) + PaletteExtraRows * 2;
        int lastVisibleRow = Mathf.Min(rowCount - 1, firstVisibleRow + visibleRows);

        GUILayout.Space(firstVisibleRow * PaletteRowHeight);

        for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int startIndex = row * columns;

                for (int column = 0; column < columns; column++)
                {
                    int spriteIndex = startIndex + column;
                    if (spriteIndex < sprites.Count)
                    {
                        DrawSpriteButton(sprites[spriteIndex], onSelected);
                    }
                    else
                    {
                        GUILayout.Space(PaletteCellWidth);
                    }
                }
            }
        }

        int hiddenBottomRows = Mathf.Max(0, rowCount - lastVisibleRow - 1);
        GUILayout.Space(hiddenBottomRows * PaletteRowHeight);
    }

    // 단일 스프라이트 썸네일 버튼을 그린다.
    private void DrawSpriteButton(Sprite sprite, System.Action<Sprite> onSelected)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(PaletteCellWidth)))
        {
            Rect buttonRect = GUILayoutUtility.GetRect(PaletteImageSize, PaletteImageSize, GUILayout.Width(PaletteImageSize), GUILayout.Height(PaletteImageSize));
            GUIContent content = new GUIContent(string.Empty, sprite == null ? "None" : sprite.name);

            EditorGUI.BeginDisabledGroup(sprite == null);
            if (GUI.Button(buttonRect, content))
            {
                onSelected(sprite);
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();

            DrawSpritePreview(buttonRect, sprite);

            string label = sprite == null ? "None" : GetShortAssetName(sprite.name);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(PaletteImageSize));
        }
    }

    // 스프라이트 썸네일을 한 번 가져오면 창 안에서 재사용한다.
    private static void DrawSpritePreview(Rect buttonRect, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Rect imageRect = new Rect(buttonRect.x + 6f, buttonRect.y + 6f, buttonRect.width - 12f, buttonRect.height - 12f);
        Rect drawRect = FitRect(imageRect, sprite.rect.width / sprite.rect.height);
        Rect textureCoords = GetTextureCoords(sprite);

        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, textureCoords, true);
    }

    private static Rect FitRect(Rect bounds, float aspect)
    {
        float width = bounds.width;
        float height = width / aspect;

        if (height > bounds.height)
        {
            height = bounds.height;
            width = height * aspect;
        }

        float x = bounds.x + (bounds.width - width) * 0.5f;
        float y = bounds.y + (bounds.height - height) * 0.5f;
        return new Rect(x, y, width, height);
    }

    private static Rect GetTextureCoords(Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;
        return new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);
    }

    // 팔레트 셀 안에서 너무 긴 에셋 이름을 짧게 표시한다.
    private static string GetShortAssetName(string assetName)
    {
        const int maxLength = 18;
        return assetName.Length <= maxLength ? assetName : assetName.Substring(0, maxLength - 3) + "...";
    }

    // 현재 대사 인덱스를 이동하고 미리보기를 갱신한다.
    private void MoveLine(int delta)
    {
        lineIndex = Mathf.Clamp(lineIndex + delta, 0, Mathf.Max(0, currentChapterLines.Count - 1));
        LoadCurrentVisualEvent();
        RefreshCurrentPreview();
    }

    // 현재 대사와 VisualEvent 미리보기를 함께 갱신한다.
    private void RefreshCurrentPreview()
    {
        PreviewDialogue();
        ApplyCurrentVisualEvent();
    }

    // 현재 대사의 텍스트를 타이핑 없이 즉시 표시한다.
    private void PreviewDialogue()
    {
        if (dialogueView == null || CurrentLine == null)
        {
            return;
        }

        if (CurrentLine.HasChoice)
        {
            dialogueView.ShowChoiceEvent();
        }
        else
        {
            dialogueView.ShowLineImmediately(CurrentLine);
        }
    }

    // 현재 VisualEvent를 미리보기 컨트롤러에 적용한다.
    private void ApplyCurrentVisualEvent()
    {
        if (previewController == null)
        {
            return;
        }

        previewController.ApplyPreview(currentVisualEvent);
    }

    private void SetBackground(Sprite sprite)
    {
        if (previewController == null)
        {
            return;
        }

        previewController.SetPreviewBackground(sprite);
    }

    private void SetCg(Sprite sprite)
    {
        if (previewController == null)
        {
            return;
        }

        previewController.SetPreviewCg(sprite);
    }

    private void AddCharacter(Sprite sprite)
    {
        if (previewController == null)
        {
            return;
        }

        previewController.AddPreviewCharacter(sprite);
    }

    private void ResetPreview()
    {
        if (previewController == null)
        {
            return;
        }

        previewController.SetPreviewBackground(null);
        previewController.SetPreviewCg(null);
        previewController.ClearPreviewCharacters();
    }

    // 현재 미리보기 화면 상태를 VisualEvent 에셋에 저장한다.
    private void SaveCurrentVisualEvent()
    {
        VisualEvent visualEvent = GetOrCreateCurrentVisualEvent();
        if (visualEvent == null || previewController == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(visualEvent);
        visualEvent.name = CurrentLine.VisualEventKey;
        serializedObject.FindProperty("key").stringValue = CurrentLine.VisualEventKey;
        serializedObject.FindProperty("background").objectReferenceValue = previewController.PreviewBackground;
        serializedObject.FindProperty("cg").objectReferenceValue = previewController.PreviewCg;

        SerializedProperty charactersProperty = serializedObject.FindProperty("characters");
        List<VisualCharacterPlacement> placements = previewController.CapturePreviewCharacters();
        charactersProperty.arraySize = placements.Count;

        for (int i = 0; i < placements.Count; i++)
        {
            SerializedProperty element = charactersProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Image").objectReferenceValue = placements[i].Image;
            element.FindPropertyRelative("AnchoredPosition").vector2Value = placements[i].AnchoredPosition;
            element.FindPropertyRelative("Scale").floatValue = placements[i].Scale;
            element.FindPropertyRelative("Visible").boolValue = placements[i].Visible;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(visualEvent);
        AssetDatabase.SaveAssets();
        currentVisualEvent = visualEvent;
    }

    // 이전 대사의 VisualEvent 값을 현재 대사 VisualEvent로 복사한다.
    private void CopyPreviousVisualEvent()
    {
        if (lineIndex <= 0 || visualEventLibrary == null)
        {
            return;
        }

        DialogueLine previousLine = currentChapterLines[lineIndex - 1];
        if (!visualEventLibrary.TryGet(previousLine.VisualEventKey, out VisualEvent previousVisualEvent))
        {
            return;
        }

        VisualEvent visualEvent = GetOrCreateCurrentVisualEvent();
        EditorUtility.CopySerialized(previousVisualEvent, visualEvent);
        visualEvent.name = CurrentLine.VisualEventKey;

        SerializedObject serializedObject = new SerializedObject(visualEvent);
        serializedObject.FindProperty("key").stringValue = CurrentLine.VisualEventKey;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(visualEvent);
        AssetDatabase.SaveAssets();
        currentVisualEvent = visualEvent;
        RefreshCurrentPreview();
    }

    // 현재 대사의 VisualEvent를 찾거나 새로 만든다.
    private VisualEvent GetOrCreateCurrentVisualEvent()
    {
        if (CurrentLine == null)
        {
            return null;
        }

        LoadCurrentVisualEvent();
        if (currentVisualEvent != null)
        {
            return currentVisualEvent;
        }

        string chapter = chapters[chapterIndex];
        string folderPath = $"Assets/Datas/Visual Events/{chapter}";
        EnsureFolder("Assets/Datas/Visual Events");
        EnsureFolder(folderPath);

        string assetPath = $"{folderPath}/{CurrentLine.VisualEventKey}.asset";
        VisualEvent visualEvent = CreateInstance<VisualEvent>();
        visualEvent.name = CurrentLine.VisualEventKey;
        AssetDatabase.CreateAsset(visualEvent, assetPath);

        SerializedObject serializedObject = new SerializedObject(visualEvent);
        serializedObject.FindProperty("key").stringValue = CurrentLine.VisualEventKey;
        serializedObject.ApplyModifiedProperties();

        AddVisualEventToLibrary(visualEvent);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return visualEvent;
    }

    // 새 VisualEvent 에셋을 라이브러리 목록에 추가한다.
    private void AddVisualEventToLibrary(VisualEvent visualEvent)
    {
        if (visualEventLibrary == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(visualEventLibrary);
        SerializedProperty eventsProperty = serializedObject.FindProperty("events");

        for (int i = 0; i < eventsProperty.arraySize; i++)
        {
            if (eventsProperty.GetArrayElementAtIndex(i).objectReferenceValue == visualEvent)
            {
                return;
            }
        }

        eventsProperty.InsertArrayElementAtIndex(eventsProperty.arraySize);
        eventsProperty.GetArrayElementAtIndex(eventsProperty.arraySize - 1).objectReferenceValue = visualEvent;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(visualEventLibrary);
    }

    // 필요한 에셋 폴더가 없으면 생성한다.
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    // 씬에 배치된 특정 타입의 첫 번째 오브젝트를 찾는다.
    private static T FindSceneObject<T>() where T : Object
    {
        foreach (T item in Resources.FindObjectsOfTypeAll<T>())
        {
            if (!EditorUtility.IsPersistent(item))
            {
                return item;
            }
        }

        return null;
    }

    // 이전에 사용한 에셋 참조를 복원하고 없으면 프로젝트에서 첫 번째 후보를 찾는다.
    private void RestoreAssetReferences()
    {
        visualEventLibrary = LoadRememberedAsset<VisualEventLibrary>(VisualEventLibraryKey);
        palette = LoadRememberedAsset<VisualEventEditorPalette>(PaletteKey);

        if (visualEventLibrary == null)
        {
            visualEventLibrary = FindFirstAsset<VisualEventLibrary>();
        }

        if (palette == null)
        {
            palette = FindFirstAsset<VisualEventEditorPalette>();
        }

        SaveAssetReferences();
    }

    // 접힘 상태를 에디터 설정에서 복원한다.
    private void RestoreFoldoutStates()
    {
        showReferences = EditorPrefs.GetBool(ShowReferencesKey, false);
        showDialogueInfo = EditorPrefs.GetBool(ShowDialogueInfoKey, false);
        showVisualEventInfo = EditorPrefs.GetBool(ShowVisualEventInfoKey, false);
    }

    // 현재 에셋 참조 경로를 에디터 설정에 저장한다.
    private void SaveAssetReferences()
    {
        SaveAssetReference(VisualEventLibraryKey, visualEventLibrary);
        SaveAssetReference(PaletteKey, palette);
    }

    // 에디터 설정에 저장된 경로로 에셋을 불러온다.
    private static T LoadRememberedAsset<T>(string key) where T : Object
    {
        string path = EditorPrefs.GetString(key, string.Empty);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }

    // 프로젝트 안에서 지정한 타입의 첫 번째 에셋을 찾는다.
    private static T FindFirstAsset<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0)
        {
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    // 에셋 참조가 있으면 경로를 저장하고 없으면 저장값을 비운다.
    private static void SaveAssetReference(string key, Object asset)
    {
        string path = asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
        EditorPrefs.SetString(key, path);
    }
}
