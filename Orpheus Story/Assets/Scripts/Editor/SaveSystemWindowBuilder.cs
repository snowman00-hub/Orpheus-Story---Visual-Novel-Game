using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SaveSystemWindowBuilder
{
    private static readonly Color OverlayColor = new Color(0.02f, 0.025f, 0.035f, 0.72f);
    private static readonly Color PanelColor = new Color(0.055f, 0.06f, 0.075f, 0.94f);
    private static readonly Color InnerColor = new Color(0.10f, 0.105f, 0.12f, 0.88f);
    private static readonly Color SlotColor = new Color(0.16f, 0.165f, 0.18f, 0.95f);
    private static readonly Color GoldColor = new Color(0.72f, 0.58f, 0.34f, 1f);
    private static readonly Color BlueColor = new Color(0.42f, 0.62f, 0.72f, 1f);
    private static readonly Color TextColor = new Color(0.88f, 0.84f, 0.76f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.62f, 0.62f, 1f);

    [MenuItem("Tools/Orpheus Story/Build Save System Window")]
    public static void Build()
    {
        GameObject root = GameObject.Find("Canvas/Save System WIndow");
        if (root == null)
        {
            Debug.LogWarning("Canvas/Save System WIndow not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Build Save System Window UI");
        ClearChildren(root.transform);
        SetupRoot(root);
        SetupCanvas(root);

        GameObject overlay = CreateRect("Dim Overlay", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Stretch(overlay.GetComponent<RectTransform>());
        AddImage(overlay, OverlayColor);

        GameObject window = CreateRect("Window Panel", root.transform, Center(), Center(), Vector2.zero, new Vector2(1180f, 760f));
        AddImage(window, PanelColor);

        AddLine("Top Accent Line", window.transform, new Vector2(0f, -14f), new Vector2(1090f, 3f), GoldColor, true);
        AddLine("Bottom Accent Line", window.transform, new Vector2(0f, 18f), new Vector2(1090f, 2f), BlueColor, false);

        CreateText("Title", window.transform, "SAVE / LOAD", Top(), Top(), new Vector2(0f, -58f), new Vector2(520f, 54f), 34f, TextAlignmentOptions.Center, TextColor);
        CreateText("Subtitle", window.transform, "Select a slot", Top(), Top(), new Vector2(0f, -96f), new Vector2(360f, 28f), 18f, TextAlignmentOptions.Center, MutedTextColor);

        GameObject close = CreateButton("Close Button", window.transform, "X", new Vector2(-52f, -56f), new Vector2(54f, 46f), 24f);
        Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f));

        GameObject tabs = CreateRect("Mode Buttons", window.transform, Top(), Top(), new Vector2(0f, -138f), new Vector2(420f, 48f));
        CreateButton("Save Mode Button", tabs.transform, "Save", new Vector2(-105f, 0f), new Vector2(190f, 42f), 22f);
        CreateButton("Load Mode Button", tabs.transform, "Load", new Vector2(105f, 0f), new Vector2(190f, 42f), 22f);

        GameObject scrollView = CreateScrollView(window.transform);
        Transform content = scrollView.transform.Find("Viewport/Content");

        for (int i = 1; i <= 8; i++)
        {
            CreateSlot(content, i);
        }

        CreateConfirmPopup(root.transform);

        if (root.GetComponent<SaveLoadWindow>() == null)
        {
            root.AddComponent<SaveLoadWindow>();
        }

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Built Save System Window UI.");
    }

    private static void CreateSlot(Transform parent, int index)
    {
        GameObject slot = CreateRect($"Save Slot {index:00}", parent, Center(), Center(), Vector2.zero, new Vector2(0f, 112f));
        AddImage(slot, SlotColor);
        slot.AddComponent<Button>();
        slot.AddComponent<SaveSlotView>();

        LayoutElement layout = slot.AddComponent<LayoutElement>();
        layout.preferredHeight = 112f;

        GameObject thumbnail = CreateRect("Thumbnail", slot.transform, LeftCenter(), LeftCenter(), new Vector2(86f, 0f), new Vector2(136f, 78f));
        AddImage(thumbnail, new Color(0.065f, 0.07f, 0.085f, 1f));

        CreateText("Slot Number", slot.transform, $"SLOT {index:00}", LeftTop(), LeftTop(), new Vector2(250f, -24f), new Vector2(160f, 24f), 18f, TextAlignmentOptions.Left, GoldColor);
        CreateText("Chapter Text", slot.transform, "No Data", LeftTop(), LeftTop(), new Vector2(300f, -53f), new Vector2(260f, 28f), 21f, TextAlignmentOptions.Left, TextColor);
        CreateText("Preview Text", slot.transform, "Saved dialogue preview will appear here.", LeftTop(), LeftTop(), new Vector2(480f, -84f), new Vector2(620f, 26f), 16f, TextAlignmentOptions.Left, MutedTextColor);
        CreateText("Saved At", slot.transform, "----.--.-- --:--", RightTop(), RightTop(), new Vector2(-126f, -31f), new Vector2(210f, 24f), 16f, TextAlignmentOptions.Right, MutedTextColor);
    }

    private static void CreateConfirmPopup(Transform parent)
    {
        GameObject popup = CreateRect("Confirm Popup", parent, Center(), Center(), Vector2.zero, new Vector2(620f, 300f));
        AddImage(popup, new Color(0.035f, 0.04f, 0.05f, 0.96f));
        popup.AddComponent<ConfirmPopup>();

        CreateText("Message", popup.transform, "Are you sure?", Top(), Top(), new Vector2(0f, -84f), new Vector2(520f, 64f), 26f, TextAlignmentOptions.Center, TextColor);

        GameObject buttons = CreateRect("Buttons", popup.transform, Bottom(), Bottom(), new Vector2(0f, 70f), new Vector2(440f, 56f));
        CreateButton("Yes Button", buttons.transform, "Yes", new Vector2(-110f, 0f), new Vector2(180f, 52f), 22f);
        CreateButton("No Button", buttons.transform, "No", new Vector2(110f, 0f), new Vector2(180f, 52f), 22f);

        popup.SetActive(false);
    }

    private static GameObject CreateScrollView(Transform parent)
    {
        GameObject scrollView = CreateRect("Slot Scroll View", parent, Center(), Center(), new Vector2(0f, -74f), new Vector2(1040f, 540f));
        AddImage(scrollView, InnerColor);
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateRect("Viewport", scrollView.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateRect("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(-48f, 0f);
        scrollRect.content = contentRect;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 24, 24);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return scrollView;
    }

    private static void SetupRoot(GameObject root)
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        Anchor(rect, Vector2.zero, Vector2.one);
        Stretch(rect);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetupCanvas(GameObject root)
    {
        CanvasScaler scaler = root.GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject button = CreateRect(name, parent, Center(), Center(), position, size);
        AddImage(button, new Color(0.12f, 0.13f, 0.15f, 0.95f));
        Button buttonComponent = button.AddComponent<Button>();
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.78f, 1f);
        colors.pressedColor = new Color(0.82f, 0.68f, 0.42f, 1f);
        colors.selectedColor = colors.highlightedColor;
        buttonComponent.colors = colors;

        GameObject label = CreateText("Label", button.transform, text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize, TextAlignmentOptions.Center, TextColor);
        Stretch(label.GetComponent<RectTransform>());
        return button;
    }

    private static GameObject CreateText(string name, Transform parent, string text, Vector2 min, Vector2 max, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = CreateRect(name, parent, min, max, position, size);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        return go;
    }

    private static void AddLine(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool top)
    {
        Vector2 anchor = top ? Top() : Bottom();
        GameObject line = CreateRect(name, parent, anchor, anchor, position, size);
        AddImage(line, color);
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        Anchor(rect, min, max);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static Image AddImage(GameObject go, Color color)
    {
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Vector2 Center() => new Vector2(0.5f, 0.5f);
    private static Vector2 Top() => new Vector2(0.5f, 1f);
    private static Vector2 Bottom() => new Vector2(0.5f, 0f);
    private static Vector2 LeftCenter() => new Vector2(0f, 0.5f);
    private static Vector2 RightCenter() => new Vector2(1f, 0.5f);
    private static Vector2 LeftTop() => new Vector2(0f, 1f);
    private static Vector2 RightTop() => new Vector2(1f, 1f);
}
