# 적 스폰 구조 결정

스포너 구현에 앞서 레퍼런스 두 프로젝트의 **적 스폰** 아키텍처를 확인하고 이 저장소에서 취할 구조를 정한 기록입니다.

- 채택: 스폰 지점은 `EnemySpawnPointContainer`(씬, 실제 구현 `EnemySpawnPointProvider`) / 생성은 `EnemySpawner` / 적 종류와 수량은 `EnemySpawnRequest`(DTO) 주입 / 지휘는 `GameDirector`

---

## 1. 레퍼런스의 적 스폰

### Boss Room — 월드에 배치된 포탈이 자기 주변에 스폰

```csharp
EnemyPortal.cs:12    // stationary dungeon element that spawns monsters when a player is nearby
EnemyPortal.cs:23    [RequireComponent(typeof(ServerWaveSpawner))]
```

스폰 주체가 전역 관리자가 아니라 던전 각 구역에 배치된 포탈입니다. 플레이어 접근을 감지해 가동하고, 연결된 브레이커블이 모두 파괴되면 휴면 상태가 됩니다.
**판단(언제 스폰할지)과 수행(생성)을 한 오브젝트가 겸합니다.**

실제 생성은 포탈이 보유한 `ServerWaveSpawner`가 담당합니다.

```csharp
ServerWaveSpawner.cs:18    NetworkObject m_NetworkedPrefab;      // 무엇을
ServerWaveSpawner.cs:22    List<Transform> m_SpawnPositions;     // 어디에 (씬 오브젝트)
ServerWaveSpawner.cs:42    int m_SpawnsPerWave = 2;              // 얼마나
```

`[SerializeField]` 15개 / 316줄. 세 가지가 한 컴포넌트에 모여 있습니다.

### Chop Chop — 적 스폰이 없다

적은 씬에 직접 배치됩니다. 플레이어 스폰만 있고, 그 지점은 씬에서 수집합니다(`SpawnSystem.cs:22`).

### 공통

**맵 내부 정보를 담는 데이터 클래스가 둘 다 없습니다.** 스폰 좌표는 전부 씬의 `Transform`입니다.
맵의 진실은 씬이고, 코드는 그것을 복제하지 않고 조회합니다.

---

## 2. 구현 방향

```
GameDirector          조건 판단 → 의뢰
     │ EnemySpawnRequest { enemyPrefab(실제 구현 필드명은 enemy입니다.), count }
     ▼
EnemySpawner          지점 선택 → NavMesh 보정 → Instantiate
     │
     ▼
EnemySpawnPointContainer(실제 구현 이름은 EnemySpawnPointProvider입니다.)   씬에 배치한 스폰 지점들의 부모
```

**① 판단은 `GameDirector`, 수행은 `EnemySpawner`.**
**② 스폰 지점은 씬에 배치하고, 컨테이너가 수집해 제공.**
**③ "어떤 적을, 몇 마리" 정보를 DTO로 요청받아 적들을 스폰.**

---

## 3. 구현 결과

```
Game/GameDirector.cs                    초기화 담당, 적 스폰 의뢰, 적의 추적 대상 주입
Enemy/Spawn/EnemySpawner.cs             스폰 지점을 라운드로빈 방식으로 샘플링, 적 prefab 오브젝트 Instantiate
Enemy/Spawn/EnemySpawnPointProvider.cs  스폰 지점 목록 검증 및 제공
Enemy/Spawn/EnemySpawnPoint.cs          적이 생성될 좌표, 생성된 적의 회전 · 생성 위치의 NavMesh 보정 · 씬 배치를 위한 개발용 Gizmo
Enemy/Spawn/EnemySpawnRequest.cs        spawner에게 생성을 요구할 때 사용되는 DTO struct
```

계획과 달라진 지점만 적습니다.

| 항목 | 계획 | 실제 |
|---|---|---|
| 스폰 지점 확보 | 컨테이너가 자식을 순회해 수집 | 인스펙터에 연결한 `List<EnemySpawnPoint>`를 검증만 |
| NavMesh 보정 | `EnemySpawner`가 수행 | `EnemySpawnPoint.TryGetSpawnPose()`가 수행 |
| 수량 상한 | 명시 없음 | 지점 개수를 넘으면 경고 후 clamp |

- **런타임에 수집하는 방식 대신 Map에서 미리 연결해두는 방법을 택했습니다.** 계층 구조에 의존하지 않으므로 테스트에서 단독으로 구성할 수 있습니다. 다만, 부차적으로 spawn point라는 class로 좀 더 세분화되게 구현을 진행했는데, 이는 추후 test code의 도입 등을 고려한 조치입니다.
- **enemy의 스폰 지점 보정을 spawn point class로 위임했습니다.** "여기에 적이 설 수 있는가"는 지점 자신의 속성이고, 보정 반경과 Gizmo 표시를 한 곳에서 소유하게 됩니다. 결과적으로 `EnemySpawner`는 NavMesh를 알지 않습니다.
- **수량을 지점 개수로 제한했습니다.** Boss Room은 지점을 무한 재사용하지만 enemy 생성에 대한 기다림 처리(`m_TimeBetweenSpawns`)가 있어 겹치지 않습니다. 제한하지 않는 경우 겹침 문제가 발생하지만, 한 지점에 여러 enemy를 spawn할 니즈가 없어서 간단하게 처리했습니다.
- **`EnemyCharacter.SetTarget()`을 추가했습니다.** 기존의 빠른 개발을 위해 사용했던 씬 오브젝트의 사전 참조 처리 방식 대신 `GameDirector`가 생성 직후 주입합니다. 게임 라이프사이클이 고도화됨에 따라 달라질 여지가 있으나, 기본적으로는 GameDirector에게 위임하는 방향성으로 개발하고자 합니다.
