# 유닛 상태 관리 권한 결정: 분산 소유 vs State 컴포넌트

이 문서는 사망 처리 착수 시점(2026-08-26)에 드러난 **유닛 상태 소유권 문제**와 그 개선 방향을 기록합니다.

- 결정: 유닛마다 **상태 소유·변경 권한을 중앙 관리하는 컴포넌트**를 둔다 (플레이어는 `PlayerCharacter`, 적은 `EnemyCharacter`).

> **이름 이력 (2026-08-30 갱신).** 이 문서를 쓸 당시 컴포넌트 이름은 `PlayerState` / `TrainingDummyState`였습니다.
> 이후 이 컴포넌트가 상태 보관을 넘어 유닛 조립과 생명주기까지 책임지게 되면서 `PlayerCharacter` / `EnemyCharacter`로 바꿨습니다.
> 학습 단계의 더미 계층은 `Enemy` 구현 완료 후 제거했으며, 같은 구조를 `EnemyCharacter`가 잇습니다.
> 본문의 `file:line`은 현재 코드 기준으로 갱신했습니다. 단 "작업 전" 표기가 붙은 표는 리팩토링 이전 코드를 가리키므로 현재 코드에는 없습니다.

---

## 1. 문제 — 유닛 상태에 주인이 없다

"이 유닛이 지금 무엇을 할 수 있는가"를 결정하는 상태가 **두 컴포넌트에 나뉘어 관리**되고 있습니다.

| 상태 | 작업 전 소유자 | 위치 (작업 전 기준) |
|---|---|---|
| 조작 잠금 (`_isControlLocked` + 타이머) | `PlayerMovement` | `:93-100`, `:133-134`, `:214` |
| Dash (`_isDashing` + 타이머) | `PlayerMovement` | `:113-118`, `:131` |
| Hit Stun (`_hitStunTimer`) | `PlayerHealth` | `:18`, `:21`, `:44` |
| Hit 리액션 쿨다운 (`_hitReactionTimer`) | `PlayerHealth` | `:17`, `:30`, `:46` |
| **생사** | **없음** | 미구현 |

이 중 Dash는 이동 계산과 결합된 값이므로 `PlayerMovement`에서 이번에 새로 만들어지는 컴포넌트로 이전할 이유는 느끼지 못했습니다.
이 문서가 지적하는 대상은 **여러 컴포넌트가 각자 읽어 조합하는 상태** — 조작 잠금, Hit 및 Stun, 이번에 추가로 구현될 사망 처리에 대한 것입니다.

그리고 이 상태를 **사용하는 쪽이 각 컴포넌트가 노출한 정보를 각자 조합**합니다.

```csharp
PlayerMovement.cs:80     if (_isDashing || _isControlLocked)                            // 회전·애니 스킵
PlayerMovement.cs:120    if (_isControlLocked) return;                                  // Dash 차단
PlayerMovement.cs:172    if (!_isControlLocked && _inputReader.JumpPressed)             // 점프 차단
PlayerMeleeAttack.cs:49  if (_movement.IsControlLocked || _health.IsInHitStun) return;  // 공격 차단
```

`PlayerMeleeAttack.cs:49`가 문제를 가장 잘 보여줍니다. **다른 두 컴포넌트의 내부 상태를 각각 읽어 직접 AND 연산**하고 있었습니다.

### 왜 지금 문제가 되는가

여기에 사망을 추가하면 `|| _health.IsDead`를 위 네 곳에 모두 붙여야 합니다.
이후 무적 시간이나 추가 Stun이 생기면 다시 네 곳입니다. **상태 N개 × 소비처 M개**로 늘어납니다.

이것은 추상화가 부족한 문제가 아니라 **상태 변경 권한이 분산되어 있는 문제**입니다.
`PlayerMovement.cs:133`처럼 상태 필드에 직접 대입하는 지점이 여러 곳에 존재하는 한, 규칙을 한 곳에서 읽을 수 없습니다.

또한 각 컴포넌트가 자기 `Update()`를 독립적으로 돌기 때문에 **Stun 타이머 감산과 공격 판정의 실행 순서가 Unity의 생명주기 함수에 의존합니다.**

이는 추후 콘텐츠 확장과 상태 관리 차원에서 문제의 여지가 있으며, 사망 상태를 추가하는 지금 이에 대한 기술 부채를 수거하기 좋은 타이밍이라고 판단했습니다.

---

## 2. 작업하기에 앞서, 참고 레퍼런스

### Boss Room — `ServerCharacter`가 유닛의 단일 State

유닛 상태(HP, `LifeState`, 액션 큐, AI 브레인)가 한 컴포넌트에 중앙화되어 관리됩니다.

```csharp
NetworkLifeState.cs:7        enum LifeState { Alive, Fainted, Dead }

ServerCharacter.cs:301       void ReceiveHP(ServerCharacter inflicter, int HP)
ServerCharacter.cs:346           LifeState = LifeState.Dead;
ServerCharacter.cs:353           m_ServerActionPlayer.ClearActions(false);
```

**데미지 → HP 감산 → 생사 판정 → 진행 중 액션 취소가 한 함수 안에서 순서대로 처리됩니다.**

값을 보관하는 곳과 값을 바꿀 수 있는 곳이 분리되어 있다는 점도 중요합니다.

```csharp
ServerCharacter.cs:74        public NetworkLifeState NetLifeState { get; private set; }
ServerCharacter.cs:79        public LifeState LifeState
ServerCharacter.cs:81            get => NetLifeState.LifeState.Value;
ServerCharacter.cs:82            private set => NetLifeState.LifeState.Value = value;   // 쓰기는 내부에서만
```

값 자체는 별도 컴포넌트인 `NetworkLifeState`가 들고 있지만, **변경 권한은 `private set`으로 `ServerCharacter` 내부에 은닉**되어 있습니다.

상태를 소비하는 쪽이 필요한 상태에 대해서 State에게 묻는 방식으로 책임을 분리하고 있습니다.

```csharp
ServerCharacter.cs:90        public bool IsValidTarget => LifeState != LifeState.Dead;
AIBrain.cs:101               potentialFoe.LifeState != LifeState.Alive        // 타깃 필터
```

애니메이션도 데미지 코드가 직접 호출하지 않고 **상태 변화를 구독**합니다.

```csharp
ServerAnimationHandler.cs:30 void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
                                 switch (newValue) { ... NetworkAnimator.SetTrigger(...) }
```

State가 소유한 객체들은 **State에 의해 tick이 수행됩니다.** 실행 순서가 State의 코드에 명시적으로 드러나므로 순서 의존을 비교적 명확하게 설계할 수 있습니다.

```csharp
ServerCharacter.cs:382       void Update()
ServerCharacter.cs:384           m_ServerActionPlayer.OnUpdate();
ServerCharacter.cs:385           if (m_AIBrain != null && LifeState == LifeState.Alive && m_BrainEnabled)
ServerCharacter.cs:387               m_AIBrain.Update();
```

### Chop Chop — 상태 저장소와 변경 권한의 분리

```csharp
Protagonist.cs:5    // This component consumes input on the InputReader and stores its values.
                    // The input is then read, and manipulated, by the StateMachine's Actions.
Protagonist.cs:15   //These fields are read and manipulated by the StateMachine actions
Damageable.cs:24    //Flags that the StateMachine uses for Conditions to move between states
```

`Protagonist`는 상태를 보관하고, 그것을 읽고 조작하는 것은 **StateMachine Actions**입니다.
다만 필드가 `public`이라 강제되지는 않습니다. 주석으로 약속된 프로젝트의 관례이며, Boss Room의 `private set`(`:82`)과는 강도가 다릅니다.

---

## 3. 개선 방향 — 유닛의 상태 관리를 책임지는 `PlayerCharacter`

유닛 상태를 단독 소유하고 변경 권한을 독점하는 컴포넌트를 둡니다.

| | 작업 전 | 개선 후 |
|---|---|---|
| 잠금 소유 | `PlayerMovement._isControlLocked` | `PlayerCharacter` |
| Stun 소유 | `PlayerHealth._hitStunTimer` | `PlayerCharacter` |
| 생사 소유 | 없음 | `PlayerCharacter` |
| 잠금 설정 | `PlayerMovement.cs:133` 필드 직접 대입 | `_state.LockControl(d)` 요청 |
| Stun 설정 | `PlayerHealth.cs:44` 필드 직접 대입 | `_state.ApplyStun(d)` 요청 |
| 행동 가능 조회 | `PlayerMeleeAttack.cs:49` 두 컴포넌트 AND | `_state.CanAttack` |
| 사망 애니 | (미구현) | `Died` 이벤트 구독 |

**핵심은 조회가 아니라 대입입니다.** 상태 필드에 직접 쓰는 지점을 없애고 전부 Character의 메서드를 통과시킵니다.
새 상태가 추가되면 Character 내부의 규칙만 고치면 되고, 소비처는 건드리지 않습니다.

---

## 4. 구현 결과

계획과 달라진 지점만 적습니다.

| 항목 | 계획 | 실제 |
|---|---|---|
| Stun 소유 | `PlayerCharacter` | `PlayerMovement` (`PlayerMovement.cs:62,214,217`) |
| 잠금 설정 | `_state.LockControl(d)` 요청 | 하위가 소유하고 Character는 사실만 조회 (`PlayerCharacter.cs:36`) |
| Stun 설정 | `_state.ApplyStun(d)` | `ReceiveDamage()` 내부로 종속 (`PlayerCharacter.cs:111`) |

- **Stun 소유자는 유닛 구성을 따릅니다.**
  - Stun은 움직임 제약이므로 이동 컴포넌트가 있으면 해당 컴포넌트가 책임을 지도록 했습니다. 다만 이동이 없던 학습용 더미는 Character 컴포넌트가 겸했습니다 (해당 계층은 `Enemy` 구현 완료 후 제거).
  - 이동을 갖춘 현재의 적은 이 규칙대로 `EnemyMovement`가 Stun을 소유합니다 (`EnemyMovement.cs:27,61,69`).

- **움직임 잠금과 Stun에 대한 값은 하위 컴포넌트가 소유하고, Character는 이를 읽어 처리합니다.**
  - 원래 계획은 Character가 해당값까지 소유하는 형태였으나, 조작 잠금 시간이 `dashDuration + dashRecoveryTime`으로 계산되는 값이라 Character가 가져가면 Dash 파라미터와 그 결과가 두 컴포넌트로 흩어집니다.
  - Character는 `IsInHitStun`·`IsControlLocked`를 사실로 읽어 행동 허용 여부만 판단합니다 (`PlayerCharacter.cs:36`).

- **Stun 처리가 Hit에 종속된다고 판단해 진입점을 `ReceiveDamage()` 하나로 줄였습니다.**
  - 다만 Stun이라는 개념을 다른 곳에서도 활용하기 용이하게 API는 별도로 유지했습니다. (감전 상태같은거 추가할 때 등)

구현하며 추가로 결정한 것:

- **유닛의 Unity 생명주기 진입점을 Character 하나로 모았습니다.**
  - 유닛 컴포넌트 중 `Awake`/`Update`/`OnDestroy`를 가진 것은 `PlayerCharacter`와 `EnemyCharacter`뿐이고, 하위는 `Init`/`Tick`/`Dispose`를 Character가 순서대로 호출합니다 (`PlayerCharacter.cs:46,51,56`, `EnemyCharacter.cs:47,52,57`).
  - 단, `PlayerInputReader`의 경우 Input System의 구독·해제가 `OnEnable`/`OnDisable`에 묶여 있고 게임 Tick에 포함될 요소가 아니라고 판단하여 기존 상태를 유지했습니다.

- **타이머를 전부 타임스탬프 방식으로 개선했습니다.** (`EnemyMeleeAttack.cs:19,57`, `PlayerMovement.cs:62,214`).
  - 남은 시간 감산은 매 프레임 갱신이 필요해 컴포넌트 구동 순서에 의존합니다.
  - 또한 해당 방식의 판정 처리를 매 프레임 갱신 방법으로 접근할 필요성이 없었습니다.

- **`Damaged`와 `Died`를 나눴습니다** (`EnemyMeleeAttack.cs:72,79`).
  - 슈퍼아머(`InterruptibleByHit = false`)는 "Hit이 안 끊는다"는 규칙이지 "죽어도 안 끊는다"가 아닙니다.

- **기존의 Health.IsDead가 혼동을 주는 변수명이어서 좀 더 적합한 IsDepleted로 개선했습니다.**
  - `IsActionAllowed`(유닛이 행동해도 되는가)와 `CanAttack`(행동이 준비됐는가)도 같은 맥락입니다.

---

## 5. 발견은 했지만 일단 넘어가는 것

- 유닛의 사망 판정을 Character의 `Tick` 폴링으로 처리하는데, 데미지 처리는 Coroutine 단계에서 도착하므로 그 프레임의 `Update`는 이미 지나간 뒤입니다. 전이가 항상 한 프레임 늦습니다. 다만 기능 구현을 위한 상태에 있어 크나큰 문제를 야기하는 요소는 아니라고 판단, 중요해지는 순간이 오기 전까지 손대지 않기로 했습니다.
  - Boss Room(`ServerCharacter.cs:341`)과 Chop Chop(`Damageable.cs:72`)의 내용을 참고하여 개선점은 찾아두고 넘어갑니다.
