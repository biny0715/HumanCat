using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라 기준 무한 타일맵 재배치 시스템.
///
/// 원리:
/// - 각 타일이 카메라 시야 + 버퍼를 벗어나면 반대쪽으로 gridSize만큼 텔레포트.
/// - 4방향(상하좌우) 모두 처리 → 플레이어 자유 이동 대응.
/// - Instantiate 없음, 위치 재배치만 사용.
///
/// 타일 순서: col-major (col0row0, col0row1, ..., col2row2)
/// </summary>
public class TileManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] int   columns  = 3;
    [SerializeField] int   rows     = 3;
    [SerializeField] float tileSize = 10.24f;   // 1024px / 100 PPU

    [Header("References")]
    [SerializeField] List<Transform> tiles;
    [SerializeField] Camera          gameCamera;

    bool running = true;

    float GridW => columns * tileSize;
    float GridH => rows    * tileSize;

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        if (!running || gameCamera == null) return;
        RecycleTiles();
    }

    // ── 재배치 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 카메라 중심으로부터 [halfW + tileSize, halfH + tileSize] 범위를 벗어난
    /// 타일을 반대 방향의 그리드 경계로 이동시킨다.
    ///
    /// 예) 플레이어가 오른쪽으로 이동
    ///   → 왼쪽으로 밀려난 타일의 dx < -(halfW + tileSize)
    ///   → tile.x += GridW  (오른쪽 끝으로 순간이동)
    /// </summary>
    void RecycleTiles()
    {
        Vector2 cam   = gameCamera.transform.position;
        float   halfH = gameCamera.orthographicSize;
        float   halfW = halfH * gameCamera.aspect;
        float   bufW  = halfW + tileSize;
        float   bufH  = halfH + tileSize;

        foreach (var tile in tiles)
        {
            float dx = tile.position.x - cam.x;
            float dy = tile.position.y - cam.y;

            if      (dx < -bufW) tile.position += new Vector3(GridW,  0, 0);
            else if (dx >  bufW) tile.position -= new Vector3(GridW,  0, 0);

            if      (dy < -bufH) tile.position += new Vector3(0,  GridH, 0);
            else if (dy >  bufH) tile.position -= new Vector3(0,  GridH, 0);
        }
    }

    // ── 퍼블릭 API ────────────────────────────────────────────────────────

    public void SetRunning(bool value) => running = value;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (gameCamera == null) return;
        float halfH = gameCamera.orthographicSize;
        float halfW = halfH * gameCamera.aspect;
        Gizmos.color = Color.yellow;
        Vector3 c = gameCamera.transform.position;
        Gizmos.DrawWireCube(new Vector3(c.x, c.y, 0),
            new Vector3((halfW + tileSize) * 2, (halfH + tileSize) * 2, 0));
    }
#endif
}
