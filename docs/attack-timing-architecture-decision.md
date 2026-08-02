# 공격 판정-애니메이션 타이밍 관리 결정: 데이터 주도(hitDelay) 방식

이 문서는 근접 전투 구현 시점(2026-08-02)에 내린 "공격 모션과 데미지 판정 시점을 어떻게 동기화할 것인가"에 대한 결정과 그 근거를 기록합니다.

- 결정: **방식 ② 데이터 주도** — 판정 타이밍(`hitDelay`)을 `AttackConfig` ScriptableObject에 두고, 코루틴 타이머로 데미지 시점을 실행함. 애니메이션은 그 위에 얹는 표현일 뿐임.
- 기각: 방식 ① 애니메이션 주도 (Animation Event 기반의 로직 실행)
- 유보: 방식 ③ 하이브리드 (클라 예측 + 서버 판정) — 네트워크 기반 콘텐츠 작업 시에 해당 방식을 적용할 예정

## 1. 비교한 세 방식

### 방식 ① — 애니메이션 주도 (Animation Event)

클립의 임팩트 프레임에 이벤트를 심어 무기 콜라이더를 Toggle. 애니메이션과 데미지 처리의 동기화가 구조적으로 보장됨.

- 싱글 AAA 액션(솔즈류, 갓 오브 워)
- 언리얼 Anim Notify 파이프라인
- 레퍼런스 프로젝트
  - Chop Chop `Attacker.cs` — `EnableWeapon`/`DisableWeapon`을 클립 이벤트가 호출

**장점**: 모션과 판정이 어긋날 수 없음. 클립 교체 시 판정이 따라옴. 애니메이터가 전투 감각을 소유.
**단점**: 판정 로직이 클립 자산에 종속됨. 서버는 Animator를 돌리지 않으므로 서버 권위 구조로 가기 어려움. Unity 기본 Animation Event는 문자열 바인딩이라 리팩터링에 취약.

### 방식 ② — 데이터가 진실 (frame data / windup 타이머) ← 채택

"시전 후 X초에 판정"을 데이터로 정의. 트리거 발화와 동시에 쿨다운을 걸고, `hitDelay` 경과 후 판정 시점 재검사(거리/오버랩)를 거쳐 데미지를 적용함.

- 격투게임 frame data(startup/active/recovery)
- 서버 권위 게임 전반에서 채택하는 대중적인 방식
- 레퍼런스 프로젝트
  - Boss Room `ActionConfig.cs`의 `ExecTimeSeconds`/`ReuseTimeSeconds`/`Anim` + `MeleeAction.cs`의 경과 시간 비교.
  - 자산 배치도 동일 구조(`GameData/Action/Imp/ImpBaseAttack.asset` 등 — 클래스 하나, 유닛별 자산 인스턴스)를 따름

**장점**: 서버가 Animator 없이 판정 데이터(타이밍·수치)만으로 시뮬레이션 가능. 밸런싱이 코드 수정 없는 콘텐츠 작업. 애니메이션 없이도 로직 테스트 가능.
**단점**: 클립의 임팩트 시점과 `hitDelay` 값을 **사람이 손수 맞춰야 함**.

### 방식 ③ — 하이브리드 (클라 예측 + 서버 판정)

입력 즉시 클라가 선행동작 애니메이션을 재생하고, 판정은 서버가 ② 방식으로 수행 후 결과만 확정.

- 레퍼런스 프로젝트
  - Boss Room `ActionConfig.AnticipationAnim` + `MeleeAction.Client.cs` — 서버 본체(②) 위에 클라 파일 하나를 얹은 구조.
  - Co-op PvE 콘텐츠라 상태 롤백 없이 "애니메이션만 선재생"으로 충분함

## 2. 방식 ②를 택한 이유

### 이유 1: 데이터 기반 판정 방식이 추후 네트워크 기반 콘텐츠 개발에 유리함

- PvP급 기법(서버 애니메이션 리와인드, 결정론+롤백, 클립→데이터 베이크)을 조사한 결과, **어느 계보든 서버가 판정 시점에 쓰는 것은 결국 "데이터화된 애니메이션"이었음**.
- ①을 유지하는 대규모 프로젝트도 네트워크 기반 콘텐츠 개발 시 ②번 방식과 유사해지는 경향 (베이크 툴체인으로 자동 변환). 그래서 처음부터 ②로 시작하고자 했음.

### 이유 2: 1인 개발 + 무료 에셋 환경에서 ①의 장점이 유의미하지 않음

- ①의 최대 장점 "애니메이터가 전투 감각을 소유"는 전담 애니메이터가 있을 때 유지보수 관점에서 유리함. 프로그래머 1인 개발에는 해당 없음.
- ①번 방식이면 애니메이션 교체마다 클립 내 이벤트 재저작해야함, ②면 SO의 숫자 재조정으로 해결 가능. **에셋이 불안정한 1인 개발이므로 타이밍은 에셋 밖에 있는 것이 유리하다고 판단했음.**
- ②의 유지비(공격당 숫자 하나 손튜닝)는 어차피 애니메이터 미세 조정을 직접 하는 현재 상황에서 지불 가능한 비용임.

### 이유 3: 판정과 표현의 분리

- 데미지·사거리 등 **게임플레이 판정은 데이터(hitDelay)**, 검격 이펙트·사운드·발소리 등 **어긋나도 게임이 안 깨지는 표현은 Animation Event**로 분담

## 참고 자료

- Boss Room: `ActionConfig.cs`, `MeleeAction.cs`, `MeleeAction.Client.cs` (../com.unity.multiplayer.samples.coop)
- Chop Chop: `Assets/Scripts/Characters/Attacker.cs` (../open-project-1)
- [Valve Developer Wiki — Lag Compensation](https://developer.valvesoftware.com/wiki/Lag_compensation)
- [The Art of Hit Registration](https://danieljimenezmorales.github.io/2023-10-29-the-art-of-hit-registration/)
- [Game Networking Demystified Part V — Interpolation and Rollback](https://ruoyusun.com/2019/09/21/game-networking-5.html)
- [Playtank — Building Systemic Melee](https://playtank.io/2024/08/12/building-systemic-melee/)
- [What are Server-authoritative Realtime Games (Medium)](https://medium.com/wearemighty/what-are-server-authoritative-realtime-games-e2463db534d1)
