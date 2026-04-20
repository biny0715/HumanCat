using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class IndoorNightSetup
{
    [MenuItem("HumanCat/Setup Indoor Night Background")]
    static void Setup()
    {
        var indoorGo = GameObject.Find("[ Environment ]/Indoor");
        if (indoorGo == null)
        {
            Debug.LogError("[IndoorNightSetup] 'Indoor' 오브젝트를 찾지 못했습니다.");
            return;
        }

        var indoorTf = indoorGo.transform;

        // 1. Background → Background_Day 이름 변경
        var bgDay = indoorTf.Find("Background");
        if (bgDay != null)
            bgDay.gameObject.name = "Background_Day";
        else
            bgDay = indoorTf.Find("Background_Day");

        if (bgDay == null)
        {
            Debug.LogError("[IndoorNightSetup] Indoor/Background(_Day) 오브젝트를 찾지 못했습니다.");
            return;
        }

        // 2. Background_Night 이미 있으면 스킵
        var existingNight = indoorTf.Find("Background_Night");
        GameObject bgNightGo;

        if (existingNight != null)
        {
            bgNightGo = existingNight.gameObject;
            Debug.Log("[IndoorNightSetup] Background_Night 이미 존재 — 스프라이트만 재설정합니다.");
        }
        else
        {
            // 3. Background_Day 복제 → Background_Night
            bgNightGo = Object.Instantiate(bgDay.gameObject, indoorTf);
            bgNightGo.name = "Background_Night";

            // Sibling 순서: Background_Day 바로 뒤
            bgNightGo.transform.SetSiblingIndex(bgDay.GetSiblingIndex() + 1);
        }

        // 4. Indoor_Night 스프라이트 로드 및 적용
        var nightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Backgrounds/Indoor_Night.png");

        if (nightSprite == null)
        {
            Debug.LogError("[IndoorNightSetup] Assets/Art/Backgrounds/Indoor_Night.png 를 찾지 못했습니다.");
            return;
        }

        var sr = bgNightGo.GetComponent<SpriteRenderer>();
        if (sr == null) sr = bgNightGo.AddComponent<SpriteRenderer>();
        sr.sprite = nightSprite;

        // 5. 초기 상태: Night는 비활성화
        bgNightGo.SetActive(false);

        // 6. 씬 더티 마킹
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[IndoorNightSetup] 완료 — Indoor/Background_Day + Indoor/Background_Night 구성됨");
    }
}
