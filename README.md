# 인냥 (HumanCat)

Unity 2D 모바일 캐주얼 게임. 낮과 밤이 실시간으로 흐르는 세계에서 고양이를 돌보고, 밤마다 도망치는 고양이를 잡는 미니게임을 즐기며, 모은 재화로 보호소를 꾸민다.

> **플랫폼**: iOS / Android
> **엔진**: Unity 2D (URP)
> **개발 시작**: 2026년 4월 16일

---

## 플레이 영상

[![플레이 영상](https://img.youtube.com/vi/eAe0rFT4J2A/0.jpg)](https://youtube.com/shorts/eAe0rFT4J2A)

---

## 게임 개요

| 구분 | 내용 |
|------|------|
| 장르 | 2D 캐주얼 / 러너 / 인테리어 |
| 핵심 루프 | 낮(탐색·상점) → 밤(미니게임 도전) → 보상(재화/스탯) → 꾸미기 → 반복 |
| 재화 | **Gold**(라운드 보상) / **Fish**(라운드 중 픽업) |
| 시간 흐름 | 실제 1시간 = 게임 1일 (24시간 사이클) |

---

## 씬 구성

### LoginScene
앱 진입 씬 (Build Index 0). 최초 실행 여부에 따라 분기.

- **최초 실행** → 컷씬(8컷 33줄 대사) → 사용자/보호소 이름 입력 → Main
- **재실행** → 저장된 이름 표시 + 로그인 버튼 → Main
- **부트스트랩 매니저** — `CurrencyManager` / `InventoryManager` 등 `DontDestroyOnLoad` 싱글톤은 모두 LoginScene에 배치 → 이후 모든 씬에서 인스턴스 보장
- **CutsceneManager** — 상태머신(`Idle / FadingIn / Typing / WaitingDelay / WaitingInput / FadingOut / Finished`) + 페이드(CanvasGroup) + Skip 버튼
- **TypewriterEffect** — `maxVisibleCharacters` 기반 타이핑 효과 (GC 0)
- **NameInputUI** — 한글 6자 제한 3중 방어선
- **BackgroundRandomizer** — NameInput/Login 화면 진입 시 랜덤 배경 + `AspectRatioFitter.EnvelopeParent` 로 화면 cover

### Main
메인 월드 탐색 씬.

- **실외(Outdoor) / 실내(Indoor)** 전환 — 문 트리거(DoorTrigger)로 이동
- **재화 UI** — Fish / Gold 코인 텍스트 (N0 콤마 포맷) 상단 표기
- **상점** — Indoor 가구 오브젝트에 접근 시 캐릭터별 상점 UI 표시 (Human=Gold, Cat=Fish)
- **낮/밤 배경** 자동 전환 + 나이트 오버레이 페이드
- **TriggerZone_DayNight / TriggerZone_MiniGame** — 상태 전환 팝업

### MiniGame_Chase
30초 제한 추격 미니게임 씬.

- 게임 시작 전 **스탯 배분 패널** (속도·HP·저항)
- 패널에 **기대 보상 표기** — `성공: <Lv×100> Gold / 캐치 시 추가: +<Lv×100> Gold`
- 라운드 중 **희귀 FishCoin 픽업** 등장 (한 판 평균 1~2회, Fish +1)
- 30초 버티기 = 성공(Gold 지급), 고양이 캐치 = 캐치 보너스(Gold 추가)
- 게임 종료 후 **+2 게임 시간** 적용 → 아침이면 모닝 패널 → Main 복귀

---

## 시스템 설계

### 시간 시스템 (TimeManager)
```
현실 1초 = 게임 24초
현실 1시간 = 게임 1일 (24시간)
```
- 06:00 → `GameState.Day` / 18:00 → `GameState.Night`
- 앱 재실행 시 오프라인 경과 시간 자동 반영
- PlayerPrefs로 영구 저장

### 낮/밤 상태 (GameManager)
- `Day` / `Night` 두 상태, `ChangeState()`로만 변경
- `OnStateChanged` 이벤트로 전파 — SceneController, UIManager 등 구독

### 환경 전환 (SceneController)
- `Outdoor` ↔ `Indoor` — `SetActive` 방식 (씬 로드 없음, 모바일 성능 최적화)

### 재화 시스템 (CurrencyManager)
- **Fish / Gold** 두 종류, `long`(최대 100억) 사용 — `int` 범위 초과 대비
- PlayerPrefs는 `long` 미지원이라 `SetString`으로 우회 저장
- 변경 즉시 저장(`Save()`) + `OnCurrencyChanged(type, value)` 이벤트 발행
- API: `Add` / `TrySubtract` / `Set` / `Get` — 자동 Clamp(0 ~ MaxValue)
- 디자인 구분
  - **Gold** = 라운드 결과 보상(버티기/캐치)
  - **Fish** = 라운드 중 픽업, 가벼운 우연 보상

### 인벤토리 시스템 (InventoryManager + ItemData)
- **ItemData** — ScriptableObject 자산 (`Assets/Resources/Items/`)
  - `itemId`(저장 안정성), `displayName`, `icon`, 가격(Fish/Gold), 스택 규칙, `placementPrefab`, `allowedSurfaces` (`[Flags]`로 Floor/Wall)
- **InventorySlot** — `{ itemId, count }` JSON 직렬화
- **저장** — `Inventory.Data` 단일 키에 `JsonUtility`로 저장
- **부팅 시** `Resources.LoadAll<ItemData>("Items")`로 ID→자산 매핑 캐싱
- API: `TryAddItem` / `TryRemoveItem` / `CanAddItem` / `GetCount` / `ExpandMaxSlot`
- 슬롯 한도: 기본 100, 확장 가능 (하드 상한 999)
- 정합성: 로드 시 등록 안 된 itemId / count≤0 슬롯 자동 정리

### 상점 시스템 (Shop / ShopUI / ShopTrigger)
- **Shop** — Indoor 가구(`Indoor/Furniture/Objects_1_3`) 자식 두 개(Human=Gold, Cat=Fish)
  - `acceptedCurrency`로 한 가지 재화만 사용 / `Buy()`는 트랜잭션 (실패 시 잔액·인벤토리 변동 없음)
- **ShopTrigger** — Trigger Collider2D, `PlayerController.CurrentType` 기준 분기 → `OnShopOpenRequested(shop)` 정적 이벤트 발행
- **ShopUI** — `[ UI ]/HumanShop`, `[ UI ]/CatShop` 두 정적 패널
  - **사전 인스턴스화**: `ShopUIBootstrap.Start()`에서 `Initialize()` 호출 → stockList 행을 한 번만 생성 후 비활성
  - 트리거 진입 시점엔 `SetActive`만 — 인스턴스 비용 0
- **ShopItemRow** — 아이콘 + 이름 + 가격 + 구매 버튼 (재화/인벤토리 상태로 interactable 자동 갱신)

---

## 미니게임 시스템

### 스탯 (StatManager)

| 스탯 | 효과 | 단위 |
|------|------|------|
| Speed | 이동 속도 +0.3 | 포인트당 |
| HP | 최대 HP +5 | 포인트당 |
| Resistance | 충돌 시 속도 감소 완화 +2% | 포인트당 |

- 미니게임 성공 시 **레벨 +1**, **스탯 포인트 +1**
- PlayerPrefs 영구 저장

### 레벨 난이도 (LevelManager)

| 항목 | 기본값 | 증가량 |
|------|--------|--------|
| 도망 속도 | 3.0 | 레벨당 +0.15 |
| 장애물 스폰 간격 | 0.8s | 레벨당 -0.02s (최소 0.3s) |
| 게임 시간 | 30s | 10레벨마다 +5s |

### 장애물 (ObstacleManager)
- 18종 장애물 프리팹 오브젝트 풀
- 플레이어 중심 링 영역에 랜덤 스폰 / 원거리 자동 회수

### FishCoin 보너스 (FishCoinSpawner + FishCoinPickup)
- 라운드 중 어쩌다 한 번 등장하는 희귀 픽업 (한 판 평균 1~2회)
- `min/maxSpawnInterval=12~22s`, `maxSpawnsPerGame=2`
- 충돌 시 `CurrencyManager.Add(Fish, 1)` 후 자기 자신 풀 반환

### 보상 (MiniGameManager)
- 라운드 결과에 따라 **Gold** 지급
  - 버티기 성공(시간 초과) → `Lv × 100` Gold
  - 캐치 성공(고양이 접촉) → 추가 `Lv × 100` Gold (총 `Lv × 200`)
- StatPanel에 **기대 보상** 사전 표시 (UI 의존성 없이 같은 공식 공유)

### Y축 정렬 (YSort)
```
sortingOrder = baseOrder - round(Y × sortScale)
```
Y가 낮을수록 (화면 아래) 높은 order → 앞에 그려짐.

---

## 캐릭터 구조

```
CharacterBase
├── Cat (고양이 캐릭터)
└── Human
    └── MaleHuman (남자 주인공)
```

- `CharacterControllerBase` — 공통 입력 처리 추상 클래스
- `PlayerController` — 메인 씬 플레이어 조작, `CurrentType` 프로퍼티 노출 (Cat/Human)
- `MiniPlayerMover` — 미니게임 전용 이동
- `PlayerAnimator` — Idle / Walk / Run 애니메이션 전환
- 낮 = Human, 밤 = Cat (GameState 기반 자동 전환)

---

## 저장 구조 (PlayerPrefs)

| 키 | 내용 |
|----|------|
| `Login.HasInit` | 최초 실행 완료 플래그 |
| `Login.UserName` | 사용자 이름 |
| `Login.ShelterName` | 보호소 이름 |
| `GameState` | 현재 낮/밤 상태 |
| `IsIndoor` / `PlayerPosX/Y/Z` / `PlayerScaleX/Y/Z` | 위치 복원 |
| `time_gameMinutes` / `time_saveTicks` | 게임 시간 + 오프라인 경과 계산 |
| `mini_level` / `mini_statPoints` / `mini_speedStat` / `mini_hpStat` / `mini_resistStat` | 미니게임 스탯 |
| `Currency.Fish` / `Currency.Gold` | 재화 (string으로 long 직렬화) |
| `Inventory.Data` | 인벤토리 전체 JSON (`{ maxSlot, slots[] }`) |

> 키 네임스페이스: `Login.*`, `Currency.*`, `Inventory.*`, `time_*`, `mini_*` 으로 도메인 분리.

---

## 프로젝트 구조

```
Assets/
├── Animations/
├── Art/
│   ├── Backgrounds/         # Outdoor Day/Night, 야간 타일맵 배경
│   ├── Cat/                 # NormalCat / BlackCat 스프라이트
│   ├── Cutscenes/Login/     # Cut_01 ~ Cut_08
│   ├── Fonts/               # Maplestory OTF Bold/Light SDF
│   ├── Obstacle/            # 장애물 스프라이트 (18종)
│   ├── Objects/             # Portal 이미지
│   └── UI/                  # 팝업, 앱 아이콘, Quit, HumanCat_Title, HumanCat_Coin
├── Editor/
├── Prefabs/
│   ├── MiniGame/
│   │   ├── Obstacles/       # Obstacle_0 ~ Obstacle_17
│   │   └── FishCoin.prefab
│   ├── Objects/             # 인테리어 가구 프리팹 (BookCase, CatBowl, Pot, Window 등 41종)
│   └── UI/                  # ToMiniGame_Popup, Exit_Popup, ShopItemRow
├── Resources/
│   └── Items/               # ItemData ScriptableObject (자동 생성)
├── Scenes/
│   ├── LoginScene.unity
│   ├── Main.unity
│   └── MiniGame_Chase.unity
└── Scripts/
    ├── Characters/          # CharacterBase, Cat, Human 계층
    ├── Currency/            # CurrencyManager
    ├── Editor/              # DebugMenu, ShopSetupBuilder, ItemDataBuilder
    ├── Inventory/           # ItemData, InventoryManager
    ├── Login/               # LoginManager + Editor
    ├── MiniGame/            # 미니게임 로직 + FishCoinPickup, FishCoinSpawner
    │   └── Editor/          # MiniGameSceneBuilder
    ├── Shop/                # Shop, ShopTrigger, ShopUI, ShopItemRow, ShopUIBootstrap
    ├── Time/                # TimeManager
    └── UI/                  # QuitButton, MiniGamePopup, TimeUI, CurrencyUI, ShelterNameDisplay
```

---

## Editor 자동화 도구 (HumanCat 메뉴)

Unity 상단 메뉴 **HumanCat**에서 씬 설치/리셋을 자동화한다.

| 메뉴 경로 | 기능 |
|-----------|------|
| Main → Setup Main Scene | TimeManager / TimeUI / QuitButton / Popup 일괄 설치 |
| MiniGame → Setup Stat System | StatPanel / StatUI / StatManager 일괄 설치 |
| MiniGame → Setup Time System | TimeManager / MorningPanel 설치 |
| **Shop → Setup Objects_1_3 Shop** | Indoor 가구에 트리거 + Human/Cat Shop + [ UI ] 패널 + 부트스트랩 일괄 구축 |
| **Item → Generate ItemData from Prefabs/Objects** | `Assets/Prefabs/Objects` 의 모든 프리팹을 ItemData 자산으로 일괄 생성 (이미 있으면 스킵) |
| **Debug → Reset All Save Data** | 모든 PlayerPrefs 일괄 삭제 (확인 다이얼로그) |

각 매니저 우클릭 `Debug → Reset *` 컨텍스트 메뉴는 도메인별 부분 초기화에 사용.

---

## iOS 빌드

1. Unity → **File → Build Settings → iOS → Switch Platform**
2. **Player Settings** → Bundle Identifier(`com.xxx.humancat`), Team ID 설정
3. **Build** → Xcode 프로젝트 생성
4. Xcode → **Signing & Capabilities** → Team 선택 → 실기기 Run

> `QuitButton.cs`는 `#if UNITY_IOS` / `#elif UNITY_ANDROID` / `#if UNITY_EDITOR` 분기 처리 완료.
