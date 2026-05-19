# 인냥 (HumanCat)

Unity 2D 모바일 캐주얼 게임. 낮과 밤이 실시간으로 흐르는 세계에서 고양이를 돌보고, 밤마다 도망치는 고양이를 잡는 미니게임을 즐기며, 모은 재화로 보호소를 꾸민다.

> **플랫폼**: iOS / Android
> **엔진**: Unity 2D (URP)
> **개발 시작**: 2026년 4월 16일

---

## 플레이 영상

[![플레이 영상](https://img.youtube.com/vi/1yoX5TLMczk/0.jpg)](https://youtu.be/1yoX5TLMczk)

---

## 미리보기

| 메인 플레이 | 낮/밤 전환 |
|:---:|:---:|
| ![](docs/gameplay-overview.gif) | ![](docs/daynight.gif) |

| 가구 배치 + 편집 | 고양이 상점 + NPC | 미니게임 |
|:---:|:---:|:---:|
| ![](docs/placement-edit.gif) | ![](docs/cat-shop.gif) | ![](docs/minigame.gif) |

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

| 컷씬 | 이름 입력 |
|:---:|:---:|
| ![](docs/login-cutscene.PNG) | ![](docs/login-name-input.PNG) |


- **최초 실행** → 컷씬(8컷 33줄 대사) → 사용자/보호소 이름 입력 → Main
- **재실행** → 저장된 이름 표시 + 로그인 버튼 → Main
- **부트스트랩 매니저** — `CurrencyManager` / `InventoryManager` 등 `DontDestroyOnLoad` 싱글톤은 모두 LoginScene에 배치 → 이후 모든 씬에서 인스턴스 보장
- **CutsceneManager** — 상태머신(`Idle / FadingIn / Typing / WaitingDelay / WaitingInput / FadingOut / Finished`) + 페이드(CanvasGroup) + Skip 버튼
- **TypewriterEffect** — `maxVisibleCharacters` 기반 타이핑 효과 (GC 0)
- **NameInputUI** — 한글 6자 제한 3중 방어선
- **BackgroundRandomizer** — NameInput/Login 화면 진입 시 랜덤 배경 + `AspectRatioFitter.EnvelopeParent` 로 화면 cover

### Main
메인 월드 탐색 씬.

| Outdoor 낮 | Outdoor 밤 | Indoor |
|:---:|:---:|:---:|
| ![](docs/main-outdoor-day.PNG) | ![](docs/main-outdoor-night.PNG) | ![](docs/main-indoor.PNG) |


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
  - `itemId`(저장 안정성), `displayName`, `icon`, 가격(Fish/Gold), 스택 규칙, `placementPrefab`, `allowedSurfaces` (`[Flags]`로 Floor/Wall), `bottomFree` (Wall 전용 가구가 바닥 근처에서만 마그네틱 스냅되는지)
- **InventorySlot** — `{ itemId, count }` JSON 직렬화
- **Human / Cat 인벤토리 분리** — `InventorySaveData{ human, cat }` 통합 저장. PlayerController.CurrentType 기준으로 현재 인벤토리 자동 매핑
- **저장** — `Inventory.Data` 단일 키에 `JsonUtility`로 저장 (두 인벤토리 모두 포함)
- **부팅 시** `Resources.LoadAll<ItemData>("Items")`로 ID→자산 매핑 캐싱
- API: `TryAddItem` / `TryRemoveItem` / `CanAddItem` / `GetCount` / `ExpandMaxSlot` (현재 인벤토리) + `*For(PlayerType, ...)` (특정 캐릭터 지정)
- 캐릭터 전환(GameManager.OnStateChanged 구독) 시 `OnInventoryChanged` 자동 발행 → UI 자동 갱신
- 슬롯 한도: 기본 100, 확장 가능 (하드 상한 999)
- 정합성: 로드 시 등록 안 된 itemId / count≤0 슬롯 자동 정리

### 상점 시스템 (Shop / ShopUI / ShopTrigger)

| 상점 (일반 + Cat 통합) | 구매 팝업 |
|:---:|:---:|
| ![](docs/shop-cat.PNG) | ![](docs/buy-popup.PNG) |

- **Shop** — Indoor 가구(`Indoor/Furniture/Objects_1_3`) 자식 두 개(Human=Gold, Cat=Fish)
  - `acceptedCurrency`로 한 가지 재화만 사용 / `Buy()`는 트랜잭션 (실패 시 잔액·인벤토리 변동 없음)
- **ShopTrigger** — Trigger Collider2D, `PlayerController.CurrentType` 기준 분기 → `OnShopOpenRequested(shop)` 정적 이벤트 발행
  - 영역 안에서 GameState 변경 시 새 캐릭터 상점으로 자동 재발행 (Human↔Cat 즉시 교체)
  - 외부 호출용: `ForceOpen()` (GNB 상점 버튼이 호출), `static RequestCloseAll()` (ShopUI 닫기 버튼이 호출)
- **ShopUI** — `[ UI ]/HumanShop`, `[ UI ]/CatShop` 두 정적 패널
  - **사전 인스턴스화**: `ShopUIBootstrap.Start()`에서 `Initialize()` 호출 → stockList 행을 한 번만 생성 후 비활성
  - 다른 캐릭터용 shop 요청 수신 시 자기 패널은 자동 닫힘
- **ShopItemRow** — 아이콘 + 이름 + 가격 + 구매 버튼 (재화/인벤토리 상태로 interactable 자동 갱신)

### 인벤토리 UI (InventoryUI + 팝업)

![](docs/inventory-standalone.PNG)

- **두 가지 진입 모드** — 같은 패널을 모드만 바꿔 재사용
  - **Standalone** — GNB 인벤토리 버튼 클릭 시. 닫기 버튼 활성. 행 클릭 → 사용 팝업
  - **Shop** — ShopTrigger 이벤트 자동 구독. 상점과 함께 열림. 닫기 버튼 비활성. 행 클릭 → 판매 팝업
- **페이지 풀 재사용** — `itemsPerPage`(기본 6) 만큼만 ShopItemRow 사전 인스턴스화, 페이지 전환 시 Bind 재호출 (인스턴스 비용 0)
- **InventoryUIBootstrap** — 비활성 패널의 `Initialize()` 를 게임 시작 시 호출 (Awake 의존 제거)
- **SellPopupUI** — 판매 가격 = (현재 캐릭터 재화 기준) × 0.5, 0 미만은 0. 확인 → 인벤토리 차감 + Currency.Add
- **UsePopupUI** — 사용 → 인벤토리 닫기 (입력 자동 복귀). Placeable 아이템은 [배치하기] 버튼으로 `PlacementManager.TryBegin()` 호출 → 배치 모드 진입

### 가구 배치 시스템 (Placement)

| 배치 모드 | 편집 모드 |
|:---:|:---:|
| ![](docs/placement-mode.PNG) | ![](docs/edit-mode.PNG) |

- **진입**: Indoor + Human 캐릭터 한정. 인벤토리 UsePopup [배치하기] 클릭 → `PlacementManager.TryBegin(item)`
- **상태머신** — `Idle` / `Placing`. Day↔Night 전환 시 진행 중 배치 자동 취소
- **Preview**: `placementPrefab` 인스턴스를 직접 Instantiate (`PlacementPreview` 컴포넌트 부착)
  - 자식 SpriteRenderer 원본 색을 캐싱 후 `Color.Lerp(orig, tint, 0.35) + alpha 0.85` 로 valid/invalid 표시 — 원본 디테일 유지
  - 부모(`placedFurnitureRoot`) `lossyScale` 무력화 (`NormalizeScale`) — Indoor `(2,2,2)` 스케일에 영향 받지 않음
- **드래그 + 그리드 스냅** — Touchscreen/Mouse 입력(Input System), `gridSize` 단위로 정렬. 손 떼기 = **자동 확정 안 함** (사용자가 [배치] 눌러야 확정)
- **표면 검증** — `Physics2D.OverlapPoint` + `floorMask`/`wallMask`, `AllowedSurfaces` 비트 매칭
- **가구 간 충돌** — 각 가구의 **아래쪽 50%** 영역끼리 동시에 겹치면 invalid (위쪽 50%는 겹쳐도 허용 → 키 큰 가구 위에 작은 가구 올리기 등 자연스러운 레이아웃 허용)
  - 1차: `OverlapBoxAll` 후보 추출 / 2차: X 교집합 + 아래절반 Y 교집합 검사
- **마그네틱 스냅** (Wall 영역 안일 때만 동작)
  | AllowedSurfaces | BottomFree | 동작 |
  |---|---|---|
  | Floor+Wall (책장 등) | n/a | 항상 sprite 하단 → wall collider `bounds.min.y` 정렬 |
  | Wall only (창문/문) | true | `magneticBottomThreshold`(기본 1.0) 이내일 때만 정렬 |
  | Wall only (포스터 등) | false | 스냅 없음 — 자유 배치 |
- **PlacementControlsUI** — 화면 하단 [배치]/[취소]. preview valid 일 때만 [배치] interactable, invalid 사유에 따라 한국어 상태 텍스트 ("이곳에 놓을 수 없습니다" / "다른 가구와 겹칩니다")
- **확정 시** — 인벤토리 1개 차감 + `PlacementRepository.Add(itemId, pos)` 저장 + `Furniture` 레이어 적용
- **자동 BoxCollider2D 부착** (`EnsureFurnitureCollider`) — sprite 결합 bounds 기준, `isTrigger=true` (캐릭터 이동을 막지 않고 충돌 검사 전용)
- **영구 저장 + 복원**
  - `PlacementRepository` — `Furniture.Placements` 키에 `JsonUtility` 직렬화 (`itemId`, position)
  - `PlacementRestorer` — 씬 진입 시 자동 재배치 + NormalizeScale + EnsureFurnitureCollider 호출
- **레이어**: `Floor`, `Wall`, `Furniture` — `PlacementSetupBuilder` 가 TagManager 에 자동 추가

### 고양이 NPC + CatShop 시스템
Indoor 에서 자율 이동하는 NPC 고양이를 구매/판매. **Cat 은 "엔티티"** — 인벤토리와 완전 분리.

![](docs/cat-remove-popup.PNG)


- **CatItemData** — `ScriptableObject` 직접 상속 (ItemData 와 별개, 가구 필드 없음). 필드: `itemId`, `displayName`, `icon`, `fishPrice`, `catPrefab`, `isCat`. 자산 위치: `Assets/Resources/CatItems/`
- **3종 Variant** — `BlackCat` / `CheeseCat` / `SleepCat` prefab + 대응 CatItemData (fishPrice=5)
- **CatManager** — 싱글톤 (씬 한정, DontDestroyOnLoad X — catRoot fake-null 회피)
  - `SpawnCat(CatItemData)` — Indoor + Floor 마스크 안 랜덤 위치 + `NormalizeScale` (부모 scale 무력화) + Save
  - `RemoveCat(CatNPC)` — 가장 가까운 매칭 레코드 1개 제거 + Save
  - `HasCat(itemId)` — 보유 여부 (Shop 의 AlreadyOwned 검사용)
  - `OnCatChanged` 이벤트 — Spawn/Remove 시 발화 → ShopUI 자동 갱신
  - PlayerPrefs `CatNpc.Data` 키에 `[{ itemId, x, y, z }]` JSON 저장
- **CatNPCController** — Indoor 전용 AI. 상태머신(`Idle`/`Move`), Floor 마스크 안 랜덤 목적지, Idle 머리 좌우 turn, `isMoving` Animator 파라미터. 인스펙터 노출: moveSpeed, moveRadius, idleTime, headTurn 등
- **CatNPC** — 식별(itemId) + `Sell()` API. 클릭 감지는 분리
- **CatNPCClickDispatcher** — 클릭 입력 중앙 처리
  - SpriteRenderer.bounds + `spriteBoundsExpand` 패딩으로 hit 영역 확대 (작은 collider 보완)
  - **Cat 플레이어 한정** 더블클릭 timing (`doubleClickWindow` 기본 0.3s)
    - NPC 위 첫 클릭: 캐릭터 이동 보류(pending)
    - timeout 안 두 번째 NPC 클릭: 판매 팝업
    - timeout 경과: 단일 클릭 확정 → 캐릭터 이동
  - Human 플레이어 또는 NPC 가 아닌 곳 클릭: 즉시 이동
- **CatRemovePopupUI** — 판매 확인 팝업. Singleton lazy lookup (`FindAnyObjectByType(Include Inactive)`) — 비활성 시작도 OK. 판매 시 `fishPrice/2` 환급
- **Shop 확장** — `catStockList: List<CatItemData>` 별도 필드 + `CanBuyCat` / `BuyCat` API. `BuyResult.AlreadyOwned` 추가 (1 종류당 1마리 제한, count>1 도 거부)
- **ShopUI / ShopItemRow / BuyPopupUI** — 일반 + Cat 한 페이지에 합쳐 표시. `BindCat`, `OnBuyCatRequested`, `ShowCat` 오버로드. Cat 일 때 +/− 버튼 비활성(1마리 고정)
- **자동 셋업** — `HumanCat → Cat NPC` 메뉴 4개: `Build CatNPC Prefab`, `Build Cat Variants`, `Setup CatManager`, `Setup CatRemovePopup UI`

### UI 입력 차단 (UIBlocker)
- 열린 UI 개수를 카운트(`Acquire`/`Release`) 하고 `lockCount > 0` 시 `PlayerController.SetInputEnabled(false)` 자동 호출
- ShopUI / InventoryUI / 팝업이 OnEnable/OnDisable 에서 짝지어 호출 → 카운트 0 으로 떨어지면 자동 입력 복구
- 모든 UI 가 같은 매니저를 공유하므로 race-free
- LoginScene 에 GameObject 1개 배치 (DontDestroyOnLoad)

### GNB 액션 아이콘
- **`GNB/InventoryBtn`** (좌하단) — `InventoryIcon.png`, 클릭 시 `InventoryUI.OpenStandalone()`
- **`GNB/ShopBtn`** (우하단) — `ShopIcon.png`, **Indoor 상태에서만 표시** (SceneController.OnEnvironmentChanged 구독), 클릭 시 `ShopTrigger.ForceOpen()`
- `InventoryOpenButton` / `ShopOpenButton` 컴포넌트가 인스펙터에서 target 슬롯만 받아 클릭 위임

---

## 미니게임 시스템

| 스탯 배분 패널 | 라운드 진행 |
|:---:|:---:|
| ![](docs/minigame-stat-panel.PNG) | ![](docs/minigame-gameplay.PNG) |

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
| `Inventory.Data` | 인벤토리 전체 JSON (`{ human: { maxSlot, slots[] }, cat: { ... } }`) |
| `Furniture.Placements` | 배치된 가구 목록 JSON (`{ entries: [{ itemId, x, y }] }`) |
| `CatNpc.Data` | 스폰된 고양이 NPC 목록 JSON (`{ cats: [{ itemId, x, y, z }] }`) |

> 키 네임스페이스: `Login.*`, `Currency.*`, `Inventory.*`, `Furniture.*`, `CatNpc.*`, `time_*`, `mini_*` 으로 도메인 분리.

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
│   └── UI/                  # 팝업, 앱 아이콘, Quit, HumanCat_Title, HumanCat_Coin, InventoryIcon, ShopIcon
├── Editor/
├── Prefabs/
│   ├── CatNPC/              # CatNPC(base), BlackCat, CheeseCat, SleepCat
│   ├── MiniGame/
│   │   ├── Obstacles/       # Obstacle_0 ~ Obstacle_17
│   │   └── FishCoin.prefab
│   ├── Objects/             # 인테리어 가구 프리팹 (BookCase, CatBowl, Pot, Window 등 41종)
│   └── UI/                  # ToMiniGame_Popup, Exit_Popup, ShopItemRow, InventoryItemRow, InventoryPanel, SellPopup, UsePopup, CatShop, HumanShop
├── Resources/
│   ├── Items/               # ItemData ScriptableObject (자동 생성)
│   └── CatItems/            # CatItemData ScriptableObject (Cat_BlackCat / Cat_CheeseCat / Cat_SleepCat)
├── Scenes/
│   ├── LoginScene.unity
│   ├── Main.unity
│   └── MiniGame_Chase.unity
└── Scripts/
    ├── Cat/                 # CatItemData, CatManager, CatNPC, CatNPCController, CatNPCClickDispatcher, CatRemovePopupUI
    │   └── Editor/          # CatNPCSetupBuilder
    ├── Characters/          # CharacterBase, Cat, Human 계층
    ├── Currency/            # CurrencyManager
    ├── Editor/              # DebugMenu, ShopSetupBuilder, ItemDataBuilder, InventorySetupBuilder, PlacementSetupBuilder
    ├── Inventory/           # ItemData, InventoryManager, InventoryUI, InventoryItemRow, InventoryUIBootstrap, SellPopupUI, UsePopupUI
    ├── Login/               # LoginManager + Editor
    ├── MiniGame/            # 미니게임 로직 + FishCoinPickup, FishCoinSpawner
    │   └── Editor/          # MiniGameSceneBuilder
    ├── Placement/           # PlacementManager, PlacementPreview, PlacementControlsUI, PlacementRepository, PlacementRestorer, EditModeController, FurnitureInstance, FurnitureHighlight
    ├── Shop/                # Shop, ShopTrigger, ShopUI, ShopItemRow, ShopUIBootstrap, BuyPopupUI
    ├── Time/                # TimeManager
    └── UI/                  # QuitButton, MiniGamePopup, TimeUI, CurrencyUI, ShelterNameDisplay, UIBlocker, InventoryOpenButton, ShopOpenButton, ModeGatedButton
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
| **Shop → Apply Maplestory Fonts to ShopUI** | ShopUI 패널 + 행 프리팹의 모든 TMP_Text 에 한글 폰트 적용 |
| **Shop → Mirror HumanShop Design to CatShop** | HumanShop 디자인을 CatShop 으로 복제 (슬롯은 자동으로 Shop_Cat 으로 재연결) |
| **Inventory → Setup Inventory UI (ALL)** | InventoryPanel + SellPopup + UsePopup + 행 프리팹 + 부트스트랩 + LoginScene UIBlocker 일괄 구축 |
| **Inventory → Setup GNB Icons (Inventory + Shop)** | GNB 좌하단 인벤토리 / 우하단 상점 아이콘 버튼 자동 배치 (이미지 적용 + 슬롯 연결) |
| **Item → Generate ItemData from Prefabs/Objects** | `Assets/Prefabs/Objects` 의 모든 프리팹을 ItemData 자산으로 일괄 생성 (이미 있으면 스킵) |
| **Placement → Add Placement Layers** | Floor / Wall / Furniture 레이어를 TagManager 에 자동 추가 |
| **Placement → Setup PlacementManager (Main scene)** | Main 씬에 PlacementManager + PlacementRestorer 배치 + LayerMask 자동 연결 |
| **Placement → Setup PlacementControls UI (Main scene)** | 화면 하단 [배치]/[취소] 컨트롤 UI 일괄 구축 |
| **Cat NPC → Build CatNPC Prefab** | base CatNPC.prefab 생성 (SpriteRenderer/Animator/Rigidbody2D/CircleCollider2D/CatNPCController/CatNPC/YSort) |
| **Cat NPC → Build Cat Variants (prefab + CatItemData)** | BlackCat/CheeseCat/SleepCat 3종 prefab + CatItemData 자산 일괄 생성 (fishPrice=5) |
| **Cat NPC → Setup CatManager (Main scene)** | [ Managers ]/CatManager + [ Environment ]/Indoor/Cats 부모 + Resources/CatItems 폴더 + CatNPCClickDispatcher 일괄 셋업 |
| **Cat NPC → Setup CatRemovePopup UI (Main scene)** | 고양이 판매 확인 팝업 UI 자동 구축 |
| **Debug → Reset All Save Data** | 모든 PlayerPrefs 일괄 삭제 (확인 다이얼로그) |
| **Debug → Set Game Time to 17-50** | 게임 시간을 17:50 으로 설정 (Play 중이면 즉시, Edit 모드면 PlayerPrefs 갱신) |
| **Debug → Add 1000 Fish + 1000 Gold** | 양쪽 재화에 +1000 (Play / Edit 모드 모두 동작) |

각 매니저 우클릭 `Debug → Reset *` 컨텍스트 메뉴는 도메인별 부분 초기화에 사용.

---

## iOS 빌드

1. Unity → **File → Build Settings → iOS → Switch Platform**
2. **Player Settings** → Bundle Identifier(`com.xxx.humancat`), Team ID 설정
3. **Build** → Xcode 프로젝트 생성
4. Xcode → **Signing & Capabilities** → Team 선택 → 실기기 Run

> `QuitButton.cs`는 `#if UNITY_IOS` / `#elif UNITY_ANDROID` / `#if UNITY_EDITOR` 분기 처리 완료.
