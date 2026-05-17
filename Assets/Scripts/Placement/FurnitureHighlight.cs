using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택된 가구에 외곽선 효과를 입히는 컴포넌트.
///
/// [방식]
/// - 셰이더 의존성 없이, 자식 SpriteRenderer 마다 동일 sprite 를
///   상/하/좌/우 1px 만큼 오프셋한 복제본 4개를 sortingOrder-1 로 깔아
///   "외곽선" 효과를 낸다 (sprite 의 transparent 영역에는 외곽선이 안 그려지므로
///   sprite 모양 그대로의 외곽선이 나옴).
/// - Show 시 생성, Hide 시 파괴 — 비선택 가구에는 비용 0.
/// - 중복 생성 방지 (Show 두 번 호출해도 outline 은 1세트만).
/// </summary>
[DisallowMultipleComponent]
public class FurnitureHighlight : MonoBehaviour
{
    static readonly Color DefaultOutlineColor = new Color(1f, 0.85f, 0.2f, 1f); // 노랑
    const float DefaultOutlinePixelWidth = 2f;

    Color outlineColor       = DefaultOutlineColor;
    float outlinePixelWidth  = DefaultOutlinePixelWidth;

    readonly List<GameObject> outlineCopies = new List<GameObject>();
    bool shown;

    public bool IsShown => shown;

    public void Configure(Color color, float pixelWidth)
    {
        outlineColor      = color;
        outlinePixelWidth = Mathf.Max(0.5f, pixelWidth);
    }

    public void Show()
    {
        if (shown) return;

        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (sr == null || sr.sprite == null) continue;
            float unitsPerPixel = 1f / Mathf.Max(0.0001f, sr.sprite.pixelsPerUnit);
            float offset        = unitsPerPixel * outlinePixelWidth;

            CreateCopy(sr, new Vector3( offset,  0f, 0f));
            CreateCopy(sr, new Vector3(-offset,  0f, 0f));
            CreateCopy(sr, new Vector3( 0f,  offset, 0f));
            CreateCopy(sr, new Vector3( 0f, -offset, 0f));
        }
        shown = true;
    }

    public void Hide()
    {
        for (int i = 0; i < outlineCopies.Count; i++)
        {
            var go = outlineCopies[i];
            if (go != null) Destroy(go);
        }
        outlineCopies.Clear();
        shown = false;
    }

    void CreateCopy(SpriteRenderer source, Vector3 localOffset)
    {
        var go = new GameObject("Outline");
        go.transform.SetParent(source.transform, false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        go.layer = source.gameObject.layer;

        var copy = go.AddComponent<SpriteRenderer>();
        copy.sprite          = source.sprite;
        copy.flipX           = source.flipX;
        copy.flipY           = source.flipY;
        copy.color           = outlineColor;
        copy.sortingLayerID  = source.sortingLayerID;
        copy.sortingOrder    = source.sortingOrder - 1; // 본체 뒤에 깔림
        copy.drawMode        = source.drawMode;
        if (source.drawMode != SpriteDrawMode.Simple)
            copy.size = source.size;

        outlineCopies.Add(go);
    }

    void OnDestroy() => Hide();
}
