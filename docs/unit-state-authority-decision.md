# 유닛 상태 관리 권한 결정: 분산 소유 vs 권한자 컴포넌트

이 문서는 사망 처리 착수 시점(2026-08-26)에 드러난 **유닛 상태 소유권 문제**와 그 개선 방향을 기록합니다.

- 결정: 유닛마다 **상태 소유·변경 권한을 독점하는 컴포넌트**를 둔다 (플레이어는 `PlayerState`, 적은 `EnemyState`).
- 기각: 현행 유지(각 기능 컴포넌트가 자기 상태를 소유), 조회 창구만 만드는 방식(파사드).

---

## 1. 문제 — 유닛 상태에 주인이 없다

"이 유닛이 지금 무엇을 할 수 있는가"를 결정하는 상태가 **세 컴포넌트에 나뉘어 소유**되어 있습니다.

| 상태 | 현재 소유자 | 위치 |
|---|---|---|
| 조작 잠금 (`_isControlLocked` + 타이머) | `PlayerMovement` | `:93-100`, `:133-134`, `:214` |
| 대시 (`_isDashing` + 타이머) | `PlayerMovement` | `:113-118`, `:131` |
| 피격 경직 (`_hitStunTimer`) | `PlayerHealth` | `:18`, `:21`, `:44` |
| 피격 리액션 쿨다운 (`_hitReactionTimer`) | `PlayerHealth` | `:17`, `:30`, `:46` |
| **생사** | **없음** | 미구현 |

이 중 대시는 이동 계산과 결합된 값이므로 `PlayerMovement`에서 이번에 새로 만들어지는 컴포넌트로 이전할 이유는 느끼지 못했습니다.
이 문서가 지적하는 대상은 **여러 컴포넌트가 각자 읽어 조합하는 상태** — 조작 잠금, 피격 및 경직, 생사여부 입니다.

그리고 이 상태를 **소비하는 쪽이 각자 조합**합니다.

```csharp
PlayerMovement.cs:80     if (_isDashing || _isControlLocked)                            // 회전·애니 스킵
PlayerMovement.cs:117    if (_isControlLocked) return;                                  // 대시 차단
PlayerMovement.cs:172    if (!_isControlLocked && _inputReader.JumpPressed)             // 점프 차단
PlayerMeleeAttack.cs:49  if (_movement.IsControlLocked || _health.IsInHitStun) return;  // 공격 차단
```

`PlayerMeleeAttack.cs:49`가 문제를 가장 잘 보여줍니다. **다른 두 컴포넌트의 내부 상태를 각각 읽어 직접 AND 연산**합니다.

### 왜 지금 문제가 되는가

여기에 사망을 추가하면 `|| _health.IsDead`를 위 네 곳에 모두 붙여야 합니다.
이후 무적 시간이나 추가 경직이 생기면 다시 네 곳입니다. **상태 N개 × 소비처 M개**로 늘어납니다.

이것은 추상화가 부족한 문제가 아니라 **상태 변경 권한이 흩어져 있는 문제**입니다.
`PlayerMovement.cs:133`처럼 상태 필드에 직접 대입하는 지점이 여러 곳에 존재하는 한, 규칙을 한 곳에서 읽을 수 없습니다.

또한 각 컴포넌트가 자기 `Update()`를 독립적으로 돌기 때문에 **경직 타이머 감산과 공격 판정의 실행 순서가 Unity의 생명주기 함수에 맡겨져 있습니다.** 현재 눈에 띄는 증상은 없으나 순서 의존이 잠재해 있습니다.

이는 추후 콘텐츠 확장과 상태 관리 차원에서 문제를 야기할 것이며, 사망 상태를 추가하는 지금 이에 대한 기술 부채를 수거하기 좋은 타이밍이라고 판단했습니다.

---

## 2. 참고 레퍼런스

### Boss Room — `ServerCharacter`가 유닛의 단일 권한자

유닛 상태(HP, `LifeState`, 액션 큐, AI 브레인)를 한 컴포넌트가 관리합니다.

```csharp
NetworkLifeState.cs:7        enum LifeState { Alive, Fainted, Dead }

ServerCharacter.cs:301       void ReceiveHP(ServerCharacter inflicter, int HP)
ServerCharacter.cs:346           LifeState = LifeState.Dead;
ServerCharacter.cs:354           m_ServerActionPlayer.ClearActions(false);
```

**데미지 → HP 감산 → 생사 판정 → 진행 중 액션 취소가 한 함수 안에서 순서대로 처리됩니다.** 상태 관리가 중앙화되어 있습니다.

값을 보관하는 곳과 값을 바꿀 수 있는 곳이 분리되어 있다는 점도 중요합니다.

```csharp
ServerCharacter.cs:74        public NetworkLifeState NetLifeState { get; private set; }
ServerCharacter.cs:79        public LifeState LifeState
ServerCharacter.cs:81            get => NetLifeState.LifeState.Value;
ServerCharacter.cs:82            private set => NetLifeState.LifeState.Value = value;   // 쓰기는 내부에서만
```

값 자체는 별도 컴포넌트인 `NetworkLifeState`가 들고 있지만, **변경 권한은 `private set`으로 `ServerCharacter` 내부에 봉인**되어 있습니다. 외부에서는 읽을 수만 있습니다. 권한 분리가 규약이 아니라 언어 차원에서 강제됩니다.

상태를 소비하는 쪽은 개별 플래그를 조합하지 않고 권한자에게 묻는 방식으로 책임을 분리하고 있습니다.

```csharp
ServerCharacter.cs:90        public bool IsValidTarget => LifeState != LifeState.Dead;
AIBrain.cs:101               potentialFoe.LifeState != LifeState.Alive        // 타깃 필터
```

애니메이션도 데미지 코드가 직접 호출하지 않고 **상태 변화를 구독**합니다.

```csharp
ServerAnimationHandler.cs:31 void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
                                 switch (newValue) { ... NetworkAnimator.SetTrigger(...) }
```

하위 컴포넌트는 자기 `Update()`를 돌지 않고 **권한자가 직접 tick** 합니다. 실행 순서가 코드에 드러나므로 컴포넌트 간 순서 의존이 생기지 않습니다.

```csharp
ServerCharacter.cs:382       void Update()
ServerCharacter.cs:384           m_ServerActionPlayer.OnUpdate();
ServerCharacter.cs:385           if (m_AIBrain != null && LifeState == LifeState.Alive && m_BrainEnabled)
ServerCharacter.cs:387               m_AIBrain.Update();
```

권한자가 사망을 소유하므로 **AI 상태 enum에는 `DEAD`가 없습니다** (`AIBrain.cs:15` — `{ ATTACK, IDLE }`).
`:385`의 가드가 그 경계입니다. 죽으면 브레인을 아예 돌리지 않으므로 `AIBrain`은 "살아있을 때 무엇을 할지"만 다루면 됩니다.
이는 사망을 AI 상태로 두면 안 된다는 일반 원칙이 아니라, **상위 권한자가 있을 때 하위가 가벼워진다**는 사례입니다.

부수적으로, 사망 후 비활성화 지연도 코드 상수가 아니라 인스펙터 값입니다.

```csharp
ServerCharacter.cs:341       if (m_KilledDestroyDelaySeconds >= 0.0f && LifeState != LifeState.Dead)
ServerCharacter.cs:343           StartCoroutine(KilledDestroyProcess());
```

### Chop Chop — 상태 저장소와 변경 권한의 분리

```csharp
Protagonist.cs:5    // This component consumes input on the InputReader and stores its values.
                    // The input is then read, and manipulated, by the StateMachine's Actions.
Protagonist.cs:15   //These fields are read and manipulated by the StateMachine actions
Damageable.cs:24    //Flags that the StateMachine uses for Conditions to move between states
```

`Protagonist`는 상태를 보관하고, **변경 권한은 StateMachine이 독점**합니다.
이동·공격 컴포넌트가 자기 상태를 직접 소유하지 않습니다. 읽는 주체와 쓰는 주체가 코드 차원으로 분리되어 있습니다.

---

## 3. 개선 방향 — 유닛의 상태 관리를 책임지는 `PlayerState`

유닛 상태를 단독 소유하고 변경 권한을 독점하는 컴포넌트를 둡니다.

| | 현재 | 개선 후 |
|---|---|---|
| 잠금 소유 | `PlayerMovement._isControlLocked` | `PlayerState` |
| 경직 소유 | `PlayerHealth._hitStunTimer` | `PlayerState` |
| 생사 소유 | 없음 | `PlayerState` |
| 잠금 설정 | `PlayerMovement.cs:133` 필드 직접 대입 | `_state.LockControl(d)` 요청 |
| 경직 설정 | `PlayerHealth.cs:44` 필드 직접 대입 | `_state.ApplyStun(d)` 요청 |
| 행동 가능 조회 | `PlayerMeleeAttack.cs:49` 두 컴포넌트 AND | `_state.CanAttack` |
| 사망 애니 | (미구현) | `Died` 이벤트 구독 |

**핵심은 조회가 아니라 대입입니다.** 상태 필드에 직접 쓰는 지점을 없애고 전부 권한자의 메서드를 통과시킵니다.
새 상태가 추가되면 권한자 내부의 규칙만 고치면 되고, 소비처는 건드리지 않습니다.

이는 `CLAUDE.md` 3절에 이미 정의한 상태 머신 경계 원칙 ①과 같은 규율입니다 —
*"전이는 반드시 `ChangeState(next)` 한 지점을 통과한다. 상태 필드 직접 대입 금지."*
적 상태 머신에만 적용하려던 원칙이지만, 플레이어에도 동일하게 필요했습니다.

### 적 유닛

적은 `EnemyState`가 그대로 권한자 역할을 합니다. 플레이어와 달리 상태 전이가 있으므로 내부에 상태 머신을 겸합니다.
Boss Room과 달리 상위 권한자 계층(`ServerCharacter`)이 없으므로, **`Dead`를 상태 머신 안에 유지**합니다.
`TrainingDummyHealth`는 데미지 계산만 담당하는 얇은 실행자로 남습니다.

---

## 4. 기존 결정과의 관계

[플레이어 구조 결정](./component-architecture-decision.md)에서 기각한 **방식 A(중앙 `Player.cs`)로 되돌아가는 것이 아닙니다.** 중앙화하는 대상이 다릅니다.

| | 방식 A (기각됨) | 이번 결정 |
|---|---|---|
| 중앙화 대상 | **로직** — 이동·공격·피격의 실행 | **상태 소유권** |
| 실행 주체 | 중앙 `Player.cs` | `PlayerMovement` / `PlayerMeleeAttack` 유지 |
| 결과 | 갓클래스 위험 | 기능 단위 분리 유지 |

Chop Chop이 정확히 이 분리입니다. `Protagonist`는 상태만 보관하고 실행은 StateMachine Actions가 합니다.

---

## 5. 남은 문제

- `PlayerHealth`와 `TrainingDummyHealth`의 구조 중복은 이번 작업으로 줄어들지만 사라지지는 않습니다. 애니메이션 상태 enum이 타입 파라미터로 올라가야 완전히 공통화되는데, 제네릭 MonoBehaviour의 인스펙터 직렬화 문제와 기존 프리팹 참조 재배선 비용이 이득보다 크다고 판단, 또한 YAGNI를 고려했을때 당장은 필요하지 않은 조치라고 판단되어 의도적으로 남깁니다.
- 컴포넌트 간 실행 순서 의존은 tick 중앙화를 하지 않는 한 여전이 잔존하는 문제이지만, 당장 문제를 야기하는 요소가 아니기에 프로젝트 고도화 된 이후 청산합니다.
