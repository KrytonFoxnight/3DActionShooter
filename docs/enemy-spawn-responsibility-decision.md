# 적 스폰 구조 결정

스포너 구현에 앞서 레퍼런스 두 프로젝트의 **적 스폰** 아키텍처를 확인하고 이 저장소에서 취할 구조를 정한 기록입니다.

- 채택: 스폰 지점은 `EnemySpawnPointContainer`(씬) / 생성은 `EnemySpawner` / 적 종류와 수량은 `EnemySpawnRequest`(DTO) 주입 / 지휘는 `GameDirector`

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
     │ EnemySpawnRequest { enemyPrefab, count }
     ▼
EnemySpawner          지점 선택 → NavMesh 보정 → Instantiate
     │
     ▼
EnemySpawnPointContainer   씬에 배치한 스폰 지점들의 부모
```

**① 판단은 `GameDirector`, 수행은 `EnemySpawner`.**
**② 스폰 지점은 씬에 배치하고, 컨테이너가 수집해 제공.**
**③ "어떤 적을, 몇 마리" 정보를 DTO로 요청받아 적들을 스폰.**
