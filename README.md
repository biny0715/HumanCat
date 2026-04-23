# 인냥 (HumanCat)

Unity 2D 모바일 캐주얼 게임. 낮과 밤이 실시간으로 흐르는 세계에서 고양이를 돌보고, 밤마다 도망치는 고양이를 잡는 미니게임을 즐긴다.

> **플랫폼**: iOS / Android
> **엔진**: Unity 2D (URP)
> **개발 시작**: 2026년 4월 16일

---

## 게임 개요

| 구분 | 내용 |
|------|------|
| 장르 | 2D 캐주얼 / 러너 |
| 핵심 루프 | 낮(탐색·휴식) → 밤(미니게임 도전) → 레벨업 → 반복 |
| 시간 흐름 | 실제 1시간 = 게임 1일 (24시간 사이클) |

---

## 씬 구성

### Main
메인 월드 탐색 씬.

- **실외(Outdoor) / 실내(Indoor)** 전환 — 문 트리거(DoorTrigger)로 이동
- **낮/밤 배경** 자동 전환 + 나이트 오버레이 페이드 효과 (0.8초)
- **TriggerZone_DayNight** — 낮↔밤 전환 팝업 (시간을 06:00 또는 18:00으로 클램프)
- **TriggerZone_MiniGame** — 밤+실외 조건일 때만 활성화, 확인 시 미니게임 씬 이동
- **TimeUI** — 현재 게임 시간(HH:mm) 및 낮/밤 표시
- **QuitButton** — 플랫폼별 종료 처리 (iOS / Android / Editor)

### MiniGame_Chase
30초 제한 추격 미니게임 씬.

- 게임 시작 전 **스탯 배분 패널** (속도·HP·저항) 표시
- 플레이어가 도망치는 검은 고양이(TargetDummy)에 **접촉하면 성공**
- 시간 초과 또는 HP 0이 되면 실패
- 게임 종료 후 **+2 게임 시간** 적용
- 아침(06:00)이 되면 모닝 패널 → Main 씬으로 자동 복귀

---

## 시스템 설계

### 시간 시스템 (TimeManager)
```
현실 1초 = 게임 24초
현실 1시간 = 게임 1일 (24시간)
```
- 06:00 → `GameState.Day` 전환 / 18:00 → `GameState.Night` 전환
- 앱 재실행 시 오프라인 경과 시간 자동 반영
- PlayerPrefs로 현재 시간 영구 저장

### 낮/밤 상태 (GameManager)
- `Day` / `Night` 두 상태, `ChangeState()`로만 변경
- `OnStateChanged` 이벤트로 전파 — SceneController, UIManager, MiniGameTriggerZone 등 구독
- PlayerPrefs로 앱 재실행 후에도 상태 유지

### 환경 전환 (SceneController)
- `Outdoor` ↔ `Indoor` — `SetActive` 방식 (씬 로드 없음, 모바일 성능 최적화)
- Day/Night에 따라 배경 GameObject 교체 + 오버레이 페이드

---

## 미니게임 시스템

### 스탯 (StatManager)

| 스탯 | 효과 | 단위 |
|------|------|------|
| Speed | 이동 속도 +0.3 | 포인트당 |
| HP | 최대 HP +5 | 포인트당 |
| Resistance | 충돌 시 속도 감소 완화 +2% | 포인트당 |

- 미니게임 성공 시 **레벨 +1**, **스탯 포인트 +1**
- PlayerPrefs로 영구 저장

### 레벨 난이도 (LevelManager)

| 항목 | 기본값 | 증가량 |
|------|--------|--------|
| 도망 속도 | 3.0 | 레벨당 +0.15 |
| 장애물 스폰 간격 | 0.8s | 레벨당 -0.02s (최소 0.3s) |
| 게임 시간 | 30s | 10레벨마다 +5s |

### 장애물 (ObstacleManager)
- 18종 장애물 프리팹 오브젝트 풀 (타입별 서브풀)
- 플레이어 중심 링 영역에 랜덤 스폰 / 원거리 자동 회수
- `SortingOrder: 1` — 배경 타일(Order 0) 위에 항상 렌더링

### Y축 정렬 (YSort)
```
sortingOrder = baseOrder - round(Y × sortScale)
```
Y가 낮을수록 (화면 아래) 높은 order → 앞에 그려짐. 플레이어·TargetDummy에 적용.

---

## 캐릭터 구조

```
CharacterBase
├── Cat (고양이 캐릭터)
└── Human
    └── MaleHuman (남자 주인공)
```

- `CharacterControllerBase` — 공통 입력 처리 추상 클래스
- `PlayerController` — 메인 씬 플레이어 조작
- `MiniPlayerMover` — 미니게임 전용 이동 (StatManager 속도 적용)
- `PlayerAnimator` — Idle / Walk / Run 애니메이션 전환

---

## 저장 구조 (PlayerPrefs)

| 키 | 내용 |
|----|------|
| `GameState` | 현재 낮/밤 상태 |
| `time_gameMinutes` | 현재 게임 시간(분) |
| `time_saveTicks` | 저장 시각 (오프라인 경과 계산용) |
| `mini_level` | 미니게임 레벨 |
| `mini_statPoints` | 남은 스탯 포인트 |
| `mini_speedStat` | 속도 스탯 누적값 |
| `mini_hpStat` | HP 스탯 누적값 |
| `mini_resistStat` | 저항 스탯 누적값 |
| `IsIndoor` | 실내/실외 위치 |
| `PlayerPosX/Y/Z` | 플레이어 마지막 위치 |

---

## 프로젝트 구조

```
Assets/
├── Animations/              # BlackCat 애니메이터 컨트롤러 + 클립
├── Art/
│   ├── Backgrounds/         # Outdoor Day/Night, 야간 타일맵 배경
│   ├── Cat/
│   │   ├── NormalCat/       # 플레이어 캐릭터 스프라이트
│   │   └── BlackCat/        # 도망 고양이(TargetDummy) 스프라이트
│   ├── Fonts/               # Maplestory OTF Bold/Light SDF
│   ├── Obstacle/            # 장애물 스프라이트 아틀라스 (18종)
│   ├── Objects/             # Portal 등 오브젝트 이미지
│   └── UI/                  # 팝업, 앱 아이콘, Quit 버튼 이미지
├── Editor/                  # 씬 설치 자동화 스크립트 (HumanCat 메뉴)
├── Prefabs/
│   ├── MiniGame/Obstacles/  # Obstacle_0 ~ Obstacle_17
│   └── UI/                  # ToMiniGame_Popup, Exit_Popup
├── Scenes/
│   ├── Main.unity
│   └── MiniGame_Chase.unity
└── Scripts/
    ├── Characters/          # CharacterBase, Cat, Human 계층
    ├── MiniGame/            # 미니게임 로직 전체
    ├── Time/                # TimeManager
    └── UI/                  # QuitButton, MiniGamePopup, TimeUI
```

---

## Editor 자동화 도구 (HumanCat 메뉴)

Unity 상단 메뉴 **HumanCat**에서 씬 설치를 자동화할 수 있다.

| 메뉴 경로 | 기능 |
|-----------|------|
| Main → Setup Main Scene | TimeManager / TimeUI / QuitButton / Popup 일괄 설치 |
| Main → Fix TriggerZone_MiniGame | MiniGameTriggerZone 컴포넌트 추가 |
| Main → Fix ToMiniGame_Popup Prefab | 프리팹 컴포넌트 교체 및 버튼 연결 |
| Main → Add World Space Text to Outdoor | 월드스페이스 텍스트 생성 |
| MiniGame → Setup Stat System | StatPanel / StatUI / StatManager 일괄 설치 |
| MiniGame → Setup Time System | TimeManager / MorningPanel / StatTimeText 설치 |
| MiniGame → Add Back Button to StatPanel | 돌아가기 버튼 추가 |
| MiniGame → Add EventSystem | EventSystem 생성 |
| Debug → Reset Level & Stats | 미니게임 저장 데이터 초기화 |
| Debug → Print Current Stats | 현재 스탯 값 콘솔 출력 |

---

## iOS 빌드

1. Unity → **File → Build Settings → iOS → Switch Platform**
2. **Player Settings** → Bundle Identifier(`com.xxx.humancat`), Team ID 설정
3. **Build** → Xcode 프로젝트 생성
4. Xcode → **Signing & Capabilities** → Team 선택 → 실기기 Run

> `QuitButton.cs`는 `#if UNITY_IOS` / `#elif UNITY_ANDROID` / `#if UNITY_EDITOR` 분기 처리 완료.
