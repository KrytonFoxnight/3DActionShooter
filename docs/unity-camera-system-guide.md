# Unity 3D 카메라 시스템 구현 가이드

이 문서는 NDC 2026 세션 **「마비노기 모바일의 카메라 시스템 설계도 - 독자적인 컴포넌트 시스템 위에서 다시 구현하는 3D 게임 카메라」**를 바탕으로, Unity 프로젝트에서 자체 카메라 기능을 구현할 때 바로 참고할 수 있도록 재구성한 가이드입니다.

- 기준 자료: Librarian card `2026-06-20_767f23` 필기본, `2026-06-20_6d0471` 전사본
- 적용 대상: Unity에서 3인칭/전투/대화/연출 카메라를 직접 제어해야 하는 프로젝트
- 핵심 관점: “카메라 기능을 하나의 거대한 클래스에 넣지 말고, 상황별 출력과 최종 선택/블렌딩을 분리한다.”

## 1. 먼저 결정할 것: Cinemachine을 쓸 것인가, 직접 만들 것인가

Cinemachine은 충분히 강력합니다. 무조건 버릴 필요는 없습니다. 다만 아래 조건이 강하면 자체 시스템을 고려할 만합니다.

### 직접 구현이 유리한 경우

- 게임 로직이 Unity `GameObject`/`MonoBehaviour` 중심이 아니라 자체 컴포넌트/시뮬레이션 계층에 있음
- 카메라가 게임 상태, 전투 상태, 자동 이동, 상호작용 대상, UI 상태와 강하게 얽힘
- “마지막 수동 조작 후 몇 초 뒤, 어떤 각도로, 어떤 감속으로 복귀”처럼 디렉션 디테일이 높음
- 카메라 로직의 내부 상태와 컨트롤 포인트를 코드에서 세밀하게 잡아야 함
- 디렉터/기획자가 런타임에서 값을 직접 조정하고 반복 확인해야 함

### Cinemachine 사용이 유리한 경우

- 카메라 요구사항이 표준적인 Follow/LookAt/Blend/Timeline 수준에 가까움
- 게임 상태와 카메라 상태의 결합이 약함
- 카메라 파이프라인을 직접 설계할 만큼의 비용을 들일 필요가 없음
- 컷씬처럼 Timeline/Playable과의 연동성이 더 중요한 영역

세션에서도 마비노기 모바일은 일반 플레이 카메라는 자체 구현했지만, 컷씬 시스템에는 Cinemachine을 적극 사용했습니다. 즉 “직접 구현 vs Cinemachine”은 전역 결정이 아니라 카메라 영역별 결정입니다.

## 2. 기본 아키텍처

추천 구조는 다음 3계층입니다.

```text
Modifier
  작은 카메라 기능의 최소 단위
  입력 CameraRigProperty를 받아 수정된 CameraRigProperty를 반환

Behaviour
  현재 게임 context를 읽고 Modifier를 조합해 상황별 CameraRigProperty를 생성
  예: FollowCamera, CombatCamera, DialogCamera, ApproachingCamera

Director / CameraComponent
  여러 Behaviour의 출력 중 현재 프레임에 사용할 값을 선택
  필요하면 블렌딩/스무딩/후처리 후 Unity Camera에 최종 적용
```

핵심은 **카메라 한 대를 여러 개 두는 것이 아니라**, 여러 Behaviour가 “후보 구도”를 계산하고 Director가 최종 구도를 하나로 확정하는 방식입니다.

## 3. CameraRigProperty부터 만든다

카메라 로직 내부에서 `Transform.position`과 `Transform.rotation`을 직접 만지기보다, 구도를 설명하는 파라미터 구조체를 먼저 둡니다.

예시:

```csharp
using UnityEngine;

public struct CameraRigProperty
{
    public Vector3 TargetPosition;
    public float Yaw;
    public float Pitch;
    public float Distance;
    public Vector3 Offset;
    public float FieldOfView;

    public static CameraRigProperty Lerp(
        CameraRigProperty a,
        CameraRigProperty b,
        float t)
    {
        return new CameraRigProperty
        {
            TargetPosition = Vector3.Lerp(a.TargetPosition, b.TargetPosition, t),
            Yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, t),
            Pitch = Mathf.Lerp(a.Pitch, b.Pitch, t),
            Distance = Mathf.Lerp(a.Distance, b.Distance, t),
            Offset = Vector3.Lerp(a.Offset, b.Offset, t),
            FieldOfView = Mathf.Lerp(a.FieldOfView, b.FieldOfView, t),
        };
    }
}
```

이 구조를 쓰면 좋은 점:

- Behaviour 간 블렌딩이 쉬움
- FOV, Distance, Offset 같은 연출 값을 따로 조정하기 쉬움
- 최종 Transform 계산 직전까지 의미 있는 값으로 디버깅 가능
- `struct` 기반이면 프레임마다 새 값을 만들어도 일반적인 class allocation보다 GC 부담이 적음

## 4. Modifier는 상태 없는 static 함수로 둔다

Modifier는 “기능의 최소 단위”입니다. 예를 들면 다음과 같습니다.

- `OrbitModifier`: 입력에 따른 회전 적용
- `CollisionModifier`: 지형/벽 충돌 방지
- `TwoShotModifier`: 두 대상이 함께 보이도록 구도 조정
- `ManualMoveModifier`: 수동 이동 입력 반영
- `AutoMoveModifier`: 자동 이동 상황의 카메라 보정

예시:

```csharp
public static class OrbitModifier
{
    public static CameraRigProperty Apply(
        CameraRigProperty rig,
        Vector2 lookInput,
        float yawSpeed,
        float pitchSpeed,
        float deltaTime)
    {
        rig.Yaw += lookInput.x * yawSpeed * deltaTime;
        rig.Pitch -= lookInput.y * pitchSpeed * deltaTime;
        rig.Pitch = Mathf.Clamp(rig.Pitch, -30f, 65f);
        return rig;
    }
}
```

Modifier 설계 원칙:

- 내부 상태를 갖지 않는다.
- 입력을 받아 새 값을 반환한다.
- 여러 Behaviour에서 재사용 가능해야 한다.
- 이름은 기능 단위로 명확하게 짓는다.
- 수식은 한 줄에 뭉개지 말고 중간 값을 풀어서 쓴다.

## 5. Behaviour는 상황별 카메라 로직을 가진다

Behaviour는 현재 게임 context를 읽고, 필요한 Modifier를 조합해 한 프레임의 후보 구도를 만듭니다.

예시 인터페이스:

```csharp
public interface ICameraBehaviour
{
    bool IsActive { get; }
    int Priority { get; }
    CameraRigProperty Evaluate(float deltaTime);
}
```

Follow 카메라 예시:

```csharp
public sealed class FollowCameraBehaviour : ICameraBehaviour
{
    private readonly IPlayerContext _player;
    private CameraRigProperty _rig;

    public bool IsActive => true;
    public int Priority => 0;

    public FollowCameraBehaviour(IPlayerContext player, CameraRigProperty defaultRig)
    {
        _player = player;
        _rig = defaultRig;
    }

    public CameraRigProperty Evaluate(float deltaTime)
    {
        _rig.TargetPosition = _player.Position;
        _rig = OrbitModifier.Apply(
            _rig,
            _player.LookInput,
            yawSpeed: 180f,
            pitchSpeed: 120f,
            deltaTime);

        return _rig;
    }
}
```

Approaching 카메라 예시:

```csharp
public sealed class ApproachingCameraBehaviour : ICameraBehaviour
{
    private readonly IPlayerContext _player;
    private readonly IInteractionContext _interaction;
    private readonly CameraRigProperty _baseRig;

    public bool IsActive =>
        _player.IsAutoMoving &&
        _interaction.HasTarget &&
        _interaction.DistanceToTarget < 8f;

    public int Priority => 20;

    public ApproachingCameraBehaviour(
        IPlayerContext player,
        IInteractionContext interaction,
        CameraRigProperty baseRig)
    {
        _player = player;
        _interaction = interaction;
        _baseRig = baseRig;
    }

    public CameraRigProperty Evaluate(float deltaTime)
    {
        var rig = _baseRig;
        rig.TargetPosition = _player.Position;

        rig = TwoShotModifier.Apply(
            rig,
            _player.Position,
            _interaction.TargetPosition);

        rig = CollisionModifier.Apply(rig);
        return rig;
    }
}
```

Behaviour 분리 기준:

- 새 상황이면 새 Behaviour를 만든다.
- 공통 기능이면 Modifier로 뺀다.
- 우선순위/블렌딩 문제면 Director에서 본다.
- 특정 상황의 조건 판단이 이상하면 해당 Behaviour 내부를 본다.

## 6. Director는 최종 선택, 블렌딩, 스무딩을 담당한다

Director는 매 프레임 활성 Behaviour를 모아 최종 카메라 구도를 결정합니다.

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class CameraDirector : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _smoothTime = 0.15f;

    private readonly List<ICameraBehaviour> _behaviours = new();
    private CameraRigProperty _currentRig;
    private Vector3 _positionVelocity;

    public void Register(ICameraBehaviour behaviour)
    {
        _behaviours.Add(behaviour);
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;

        ICameraBehaviour selected = _behaviours
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();

        if (selected == null)
            return;

        CameraRigProperty targetRig = selected.Evaluate(deltaTime);

        _currentRig = SmoothRig(_currentRig, targetRig, deltaTime);

        ApplyPostEffects(ref _currentRig, deltaTime);
        ApplyToUnityCamera(_currentRig);
    }

    private CameraRigProperty SmoothRig(
        CameraRigProperty current,
        CameraRigProperty target,
        float deltaTime)
    {
        current.TargetPosition = Vector3.SmoothDamp(
            current.TargetPosition,
            target.TargetPosition,
            ref _positionVelocity,
            _smoothTime,
            Mathf.Infinity,
            deltaTime);

        current.Yaw = Mathf.LerpAngle(current.Yaw, target.Yaw, 1f - Mathf.Exp(-10f * deltaTime));
        current.Pitch = Mathf.Lerp(current.Pitch, target.Pitch, 1f - Mathf.Exp(-10f * deltaTime));
        current.Distance = Mathf.Lerp(current.Distance, target.Distance, 1f - Mathf.Exp(-10f * deltaTime));
        current.Offset = Vector3.Lerp(current.Offset, target.Offset, 1f - Mathf.Exp(-10f * deltaTime));
        current.FieldOfView = Mathf.Lerp(current.FieldOfView, target.FieldOfView, 1f - Mathf.Exp(-10f * deltaTime));

        return current;
    }

    private void ApplyPostEffects(ref CameraRigProperty rig, float deltaTime)
    {
        // Camera Shake, Timeline Camera Effect, off-center projection 등을 이 단계에서 적용한다.
    }

    private void ApplyToUnityCamera(CameraRigProperty rig)
    {
        Vector3 rotationEuler = new Vector3(rig.Pitch, rig.Yaw, 0f);
        Quaternion rotation = Quaternion.Euler(rotationEuler);

        Vector3 backward = rotation * Vector3.back;
        Vector3 position = rig.TargetPosition + rig.Offset + backward * rig.Distance;

        _cameraTransform.SetPositionAndRotation(position, rotation);
        _camera.fieldOfView = rig.FieldOfView;
    }
}
```

실전에서는 `LateUpdate` 또는 프로젝트의 고정된 카메라 업데이트 단계에서 처리합니다. 중요한 것은 캐릭터/게임 상태 업데이트 이후에 카메라가 최종 값을 읽도록 순서를 보장하는 것입니다.

## 7. 블렌딩은 RigProperty 단위로 한다

카메라 전환이 툭 튀면 대부분 “느낌이 망가진” 버그로 보입니다. Transform만 직접 블렌딩하기보다, 의미 있는 rig 파라미터끼리 블렌딩합니다.

```csharp
CameraRigProperty blended = CameraRigProperty.Lerp(
    followRig,
    combatRig,
    combatBlendWeight);
```

블렌딩할 수 있어야 하는 값:

- Target position
- Yaw / Pitch
- Distance
- Offset
- FOV
- 화면 중심 보정값
- 연출용 weight

주의할 점:

- 각도는 `Mathf.LerpAngle`을 사용합니다.
- 위치/거리/FOV는 일반 Lerp로 충분한 경우가 많습니다.
- 최종 출력에는 별도 smoothing을 한 번 더 둘 수 있습니다.
- Behaviour 내부 블렌딩과 Director 블렌딩의 책임을 섞지 않습니다.

## 8. 후처리 단계: Transform 기반 카메라 효과

세션에서 말한 post effect는 화면 shader post-effect가 아니라, 최종 카메라 transform/projection에 얹는 효과입니다.

### Camera Shake

단순 사인 샘플링으로 흔들림을 만들 수 있습니다. 다만 프레임 타이밍에 따라 극값을 놓치면 shake가 약해 보일 수 있으므로 보정이 필요합니다.

```csharp
public static class CameraShakeModifier
{
    public static Vector3 EvaluateShake(
        float elapsed,
        float magnitude,
        float frequency)
    {
        float phase = elapsed * frequency * Mathf.PI * 2f;

        float x = Mathf.Sin(phase);
        float y = Mathf.Sin(phase * 1.37f + 0.5f);

        Vector3 direction = new Vector3(x, y, 0f);

        if (direction.sqrMagnitude < 0.01f)
            direction = new Vector3(Mathf.Sign(Mathf.Cos(phase)), 0f, 0f);

        return direction.normalized * magnitude;
    }
}
```

목표는 “프레임레이트가 달라도 타격감이 일정하게 보이는 것”입니다.

### Timeline Camera Effect

스킬/공격/차징 타이밍에 맞춰 Distance, FOV, Offset을 조작합니다.

예:

- 차징 시작: FOV를 살짝 넓힘
- 발사 직전: Distance를 당김
- 타격 순간: Shake 적용
- 종료: 기본 Distance/FOV로 복귀

### Off-center projection

UI가 화면 한쪽을 가릴 때 카메라 위치를 실제로 옮기지 않고 projection 중심을 이동시키는 방식입니다.

용도:

- 상점/대화 UI에서 캐릭터를 남은 화면 중앙에 배치
- 좌/우 패널이 큰 UI에서 플레이어 시야 유지
- 카메라 월드 위치를 움직이지 않으므로 벽/지형 충돌 문제가 덜 생김

Unity에서는 `Camera.projectionMatrix`를 직접 다룰 수 있지만, 좌표계와 행렬 규칙을 반드시 통일해야 합니다.

## 9. 런타임 튜닝 툴을 같이 만든다

카메라 개발 병목은 구현보다 반복 확인입니다.

```text
값 수정 → 플레이 확인 → 다시 수정 → 다시 확인
```

이 루프가 길면 카메라 감각을 잃습니다. 따라서 가능한 한 런타임에서 바로 값을 바꾸고 확인할 수 있는 툴을 만듭니다.

툴 설계 원칙:

- 디렉션 문장을 거의 그대로 UI label로 노출한다.
- “몇 초 동안 기본 거리로 돌아간다”처럼 의도가 보이는 표현을 쓴다.
- 디렉터/기획자가 직접 값을 만져볼 수 있게 한다.
- 값을 적용하면 즉시 게임 카메라에 반영되게 한다.
- 괜찮은 값을 찾으면 serialize/export할 수 있게 한다.

중요 규칙:

```text
카메라 설정값은 캐싱하지 말고, 사용할 때 즉시 읽는다.
```

캐싱하면 런타임 툴에서 값을 바꿔도 실제 카메라에 바로 반영되지 않습니다.

## 10. 디버깅 원칙: 시각화는 비용이 아니라 절약이다

카메라 버그는 보통 exception으로 터지지 않습니다.

- 어딘가 덜그럭거림
- 특정 상황에서 살짝 튐
- 추적 대상이 이상하게 흔들림
- 벽에 닿는 느낌이 이상함
- 전환이 부자연스러움

따라서 breakpoint보다 gizmo/overlay가 빠를 때가 많습니다.

추천 시각화:

- Target position
- 후보 Behaviour별 camera position
- 최종 selected camera path
- Collision ray/sphere cast
- 지형 높이/거리 샘플
- 현재 blend weight
- 현재 FOV/Distance/Offset
- 입력 센서값/패드 입력값
- 전환 시작/종료 프레임

예시:

```csharp
private void OnDrawGizmos()
{
    Gizmos.color = Color.green;
    Gizmos.DrawSphere(_currentRig.TargetPosition, 0.15f);

    Vector3 cameraPosition = transform.position;
    Gizmos.color = Color.cyan;
    Gizmos.DrawLine(_currentRig.TargetPosition, cameraPosition);
    Gizmos.DrawWireSphere(cameraPosition, 0.2f);
}
```

원칙:

```text
시각화 코드 작성 시간 < 시각화 없이 원인을 찾느라 날리는 시간
```

## 11. 좌표계와 행렬 규칙을 명확히 한다

카메라 코드는 좌표계 문제가 매우 쉽게 섞입니다.

주의할 것:

- World space
- View space
- Screen space
- Projection matrix
- Row-major / Column-major
- Left-handed / Right-handed coordinate system

실전 권장:

- 계산은 가능하면 World space 또는 View space에서 한다.
- Screen space 계산은 마지막 수단으로 둔다.
- 프로젝트 내부 수학 라이브러리의 major/handedness를 명시한다.
- Unity에 넘기기 직전에 Unity 규칙으로 변환한다.
- projection/view matrix를 직접 만들 경우 Unity 결과와 테스트 비교한다.

세션의 실무 교훈은 “Major는 transpose로 해결할 수 있지만, Handedness는 단순 transpose로 해결되지 않는다”입니다. 좌표계가 한 번 잘못 섞이면 shadow cascade, post effect, projection 쪽에서 이상한 문제가 날 수 있습니다.

## 12. 코드 리뷰 체크리스트

카메라 코드는 “작동은 하는데 의도를 읽기 어려운 코드”가 되기 쉽습니다. 리뷰 때 아래를 봅니다.

- 단문자 변수명을 피했는가?
- 수식을 한 줄에 몰아넣지 않았는가?
- 중간 계산값에 의미 있는 이름을 붙였는가?
- 사람이 읽는 주석으로 의도를 설명했는가?
- Modifier가 상태를 갖지 않는가?
- Behaviour와 Director의 책임이 섞이지 않았는가?
- 설정값을 캐싱해서 런타임 튜닝을 막고 있지 않은가?
- 좌표계/행렬 변환 지점이 한 곳으로 모였는가?
- Gizmo/overlay로 이상한 값을 눈으로 확인할 수 있는가?

나쁜 예:

```csharp
p = t + r * (d + f(x)) + o;
```

나은 예:

```csharp
Vector3 targetPosition = target.Position;
Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
Vector3 cameraBackward = cameraRotation * Vector3.back;
Vector3 distanceOffset = cameraBackward * distance;
Vector3 cameraPosition = targetPosition + distanceOffset + shoulderOffset;
```

## 13. 구현 순서 추천

### 1단계: 최소 Follow 카메라

- `CameraRigProperty` 정의
- `FollowCameraBehaviour` 작성
- `CameraDirector`에서 Unity Camera에 적용
- Gizmo로 target/camera line 표시

### 2단계: Modifier 분리

- Orbit
- Collision
- Distance clamp
- Pitch clamp
- TwoShot

### 3단계: 상황별 Behaviour 추가

- Follow
- Combat
- Dialog
- Approaching
- Cutscene bridge 또는 Cinemachine 연동

### 4단계: Director 고도화

- priority 선택
- blend weight
- SmoothDamp/low-pass smoothing
- fallback behaviour
- transition debug overlay

### 5단계: 런타임 튜닝 툴

- 주요 파라미터 노출
- 디렉션 문장 기반 label
- 즉시 적용
- preset save/load

### 6단계: 후처리 효과

- Camera Shake
- FOV/Distance timeline effect
- Off-center projection
- UI 상황별 camera preset override

## 14. 판단 기준 요약

자체 카메라 시스템을 만든다면 목표는 “Cinemachine을 흉내 내는 것”이 아닙니다. 목표는 프로젝트의 게임 상태와 디렉션 요구를 코드 구조 안에서 감당 가능하게 만드는 것입니다.

좋은 구조의 신호:

- 새 상황이 오면 새 Behaviour를 만들 위치가 명확함
- 공통 기능은 Modifier로 재사용됨
- 최종 선택/블렌딩은 Director에서만 처리됨
- 튜닝값은 런타임에서 바로 바꿔볼 수 있음
- 이상한 느낌은 Gizmo/overlay로 바로 드러남
- Unity Camera는 최종 transform/matrix 적용 지점으로만 사용됨

나쁜 구조의 신호:

- Follow/Combat/Dialog/Shake/Collision/UI 보정이 한 클래스에 뒤섞임
- 설정값을 바꿔도 재시작해야 확인 가능함
- 카메라가 이상한데 어떤 Behaviour가 만든 값인지 모름
- 좌표계 변환이 여러 파일에 흩어짐
- 수식이 한 줄로 압축되어 중간값을 확인할 수 없음

## 15. 이 프로젝트에 바로 적용할 최소 설계안

처음부터 큰 프레임워크를 만들지 말고, 아래 정도로 시작하는 것을 권장합니다.

```text
Camera/
  CameraRigProperty.cs
  CameraDirector.cs
  Behaviours/
    ICameraBehaviour.cs
    FollowCameraBehaviour.cs
    CombatCameraBehaviour.cs
    DialogCameraBehaviour.cs
  Modifiers/
    OrbitModifier.cs
    CollisionModifier.cs
    TwoShotModifier.cs
    ShakeModifier.cs
  Debug/
    CameraGizmoDrawer.cs
    CameraRuntimeTuningPanel.cs
```

처음 구현할 최소 기능:

- Follow 카메라 1개
- Orbit modifier 1개
- Director 선택 로직
- SmoothDamp smoothing
- Gizmo 시각화
- 런타임 distance/FOV/pitch 값 조정

그 다음 실제 요구가 생길 때 Behaviour와 Modifier를 늘리는 편이 안전합니다.

## 참고한 세션 근거

- `2026-06-20_767f23`: NDC 2026 필기본. 자체 컴포넌트 시스템, Modifier/Behaviour/Director 구조, 툴링, Camera Shake, off-center projection, 좌표계/행렬 주의사항 정리.
- `2026-06-20_6d0471`: NDC 2026 전사본. 발표 본문과 Q&A. QA/문서화, 지형 충돌과 프리셋 override, 기획자용 툴 의도, Unity Camera 컴포넌트 적용 방식, `struct` 기반 RigProperty의 GC 관련 답변 확인.

이 문서의 코드 예시는 세션 내용을 Unity/C# 적용 형태로 재구성한 예시입니다. 실제 프로젝트의 입력 시스템, 업데이트 순서, 물리/충돌 API, UI 툴킷 구조에 맞게 조정해야 합니다.
