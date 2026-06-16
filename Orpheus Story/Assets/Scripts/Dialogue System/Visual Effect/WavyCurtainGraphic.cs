using UnityEngine;
using UnityEngine.UI;

// 검은 오버레이의 오른쪽 경계를 세로 물결선으로 그리는 UI 그래픽이다.
public class WavyCurtainGraphic : MaskableGraphic
{
    [SerializeField] private int segmentCount = 48; // 물결선을 얼마나 세밀하게 그릴지 결정하는 세그먼트 수
    [SerializeField] private float revealProgress;
    [SerializeField] private float waveStrength = 0.08f; // 물결의 최대 너비를 화면 너비의 몇 퍼센트로 할지 결정하는 강도
    [SerializeField] private float waveFrequency = 18f; // 물결의 빈도를 결정하는 값. 높을수록 물결이 더 자주 반복된다.

    private float wavePhase;

    // 전환 설정값을 그래픽에 적용한다.
    public void Configure(Color coverColor, int segments, float strength, float frequency)
    {
        color = coverColor;
        segmentCount = Mathf.Max(4, segments);
        waveStrength = Mathf.Max(0f, strength);
        waveFrequency = Mathf.Max(0f, frequency);
        SetVerticesDirty();
    }

    // 현재 걷힘 진행도를 적용한다.
    public void SetReveal(float progress)
    {
        revealProgress = Mathf.Clamp01(progress);
        wavePhase = revealProgress * Mathf.PI * 2f;
        SetVerticesDirty();
    }

    // 현재 진행도에 맞춰 왼쪽 검은 영역과 오른쪽 물결 경계를 메쉬로 만든다.
    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float width = rect.width;
        float height = rect.height;
        if (width <= 0f || height <= 0f)
        {
            return;
        }

        float wavePixels = width * waveStrength;
        float edgeX = Mathf.Lerp(rect.xMax + wavePixels, rect.xMin - wavePixels, revealProgress);
        int count = Mathf.Max(4, segmentCount);

        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
            float wave = Mathf.Sin(t * waveFrequency + wavePhase) * wavePixels;
            float rightX = Mathf.Clamp(edgeX + wave, rect.xMin, rect.xMax);

            AddVertex(vertexHelper, rect.xMin, y);
            AddVertex(vertexHelper, rightX, y);
        }

        for (int i = 0; i < count; i++)
        {
            int index = i * 2;
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index + 1, index + 3, index + 2);
        }
    }

    private void AddVertex(VertexHelper vertexHelper, float x, float y)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = new Vector3(x, y, 0f);
        vertexHelper.AddVert(vertex);
    }
}
