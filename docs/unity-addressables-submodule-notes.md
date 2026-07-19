# Unity 에셋 서브모듈 관리와 Addressables 참고 노트

## 결론

현재 새 Unity 프로젝트에서 에셋을 별도 Git submodule로 빼서 관리하는 방향은 합리적입니다.

다만 이 구조는 **런타임 배포 전략**이라기보다는 **소스/권한/용량/작업 분리 전략**으로 보는 것이 안전합니다.

Addressables 세션 내용은 그대로 복붙할 정답 아키텍처라기보다, 다음 기준을 잡는 참고 자료로 쓰는 것이 좋습니다.

- 에셋을 어떤 단위로 묶을 것인가
- 어떤 에셋을 같이 로드할 것인가
- 중복 dependency를 어디까지 허용할 것인가
- Addressables group을 폴더 구조와 1:1로 맞출 필요가 있는가
- remote catalog/CDN/load path branching을 지금 도입할 필요가 있는가

- 출처: Librarian store `2026-06-20_cecc5d`, Unite Seoul 2025 Addressables 세션 전사

## 현재 상황 해석

현재 상황은 다음과 같이 볼 수 있습니다.

- 새 Unity 프로젝트를 작업 중
- 에셋을 외부로 노출하지 않음
- 에셋을 별도 submodule로 빼서 관리 중
- Addressables/AssetBundle 세션 내용을 참고해야 하는지 고민 중

이 경우 핵심 구분은 다음입니다.

```text
Git submodule 경계
= 누가 관리하고, 어디까지 checkout하고, 권한을 어떻게 줄 것인가

Addressables group 경계
= 런타임에 무엇이 같이 로드되고, 무엇이 같이 업데이트되고, 무엇이 같이 캐시될 것인가
```

둘을 반드시 1:1로 맞출 필요는 없습니다.
오히려 억지로 맞추면 구조가 과하게 복잡해질 수 있습니다.

## 참고하면 좋은 세션 포인트

### 1. 에셋은 타입별이 아니라 같이 쓰이는 단위로 묶기

세션에서 중요한 내용 중 하나는 Addressables group이나 AssetBundle을 단순히 타입별로 나누면 문제가 생길 수 있다는 점입니다.

나쁜 예시는 다음과 같습니다.

```text
EnemyPrefabs
AllTextures
AllMaterials
AllAudio
```

이렇게 나누면 적 하나를 로드하려고 했는데 적 프리팹 전체, 텍스처 전체, 머티리얼 전체가 같이 따라올 수 있습니다.

더 나은 기준은 실제 플레이 중 같이 쓰일 가능성이 높은 단위입니다.

```text
Tutorial
Stage_01
Stage_02
Biome_Forest
Character_Player
Character_Enemy_Common
Boss_Raid01
Common_UI
```

즉, 폴더 구조나 에셋 타입보다 **동시에 로드되는 사용 맥락**을 기준으로 묶는 것이 좋습니다.

- 출처: `2026-06-20_cecc5d`, transcript lines 314-379 — remapper 메모리 문제와 concurrent content grouping 설명

### 2. 중복 제거를 무조건 선으로 보지 않기

세션에서는 duplicate dependency를 공통 bundle/group으로 빼는 것이 항상 좋은 것은 아니라고 설명합니다.

예를 들어 작은 텍스트 파일이나 작은 공용 에셋 몇 개를 중복 제거하려고 거대한 공통 그룹으로 빼면, 실제로는 특정 작은 에셋 하나 때문에 큰 공통 bundle이 같이 로드될 수 있습니다.

따라서 판단 기준은 다음이 좋습니다.

```text
큰 공용 셰이더 / 큰 텍스처 / 대형 공통 머티리얼
→ 공통화 고려

작은 텍스트 / 작은 설정 파일 / 소형 중복 에셋
→ 중복 허용이 더 나을 수 있음
```

목표는 “중복 0”이 아니라 **필요할 때 필요한 것만 로드하는 것**입니다.

- 출처: `2026-06-20_cecc5d`, transcript lines 363-379 — duplicate dependency를 공통 그룹으로 옮겼을 때 오히려 remapper budget이 커질 수 있다는 설명

### 3. 서브모듈 구조와 Addressables group 구조는 분리해서 생각하기

에셋 submodule은 보통 다음 목적에 좋습니다.

- 에셋 접근 권한 분리
- 메인 코드 repo 용량 분리
- 아트/콘텐츠 작업 이력 분리
- 여러 프로젝트에서 에셋 재사용
- 메인 프로젝트와 에셋 변경 주기 분리

하지만 Addressables group은 다음 목적입니다.

- 런타임 로딩 단위 관리
- 메모리 사용량 제어
- AssetBundle dependency 제어
- 패치/캐시/카탈로그 업데이트 단위 제어

따라서 submodule 폴더 구조가 다음과 같더라도:

```text
ContentAssets/
  Characters/
  Environments/
  Props/
  Audio/
  VFX/
  SharedMaterials/
```

Addressables group은 반드시 이렇게 타입별로 맞출 필요가 없습니다.

오히려 다음처럼 런타임 기준으로 따로 잡는 것이 더 자연스럽습니다.

```text
Addressables Groups
  Common_Runtime
  Tutorial
  Stage_01
  Stage_02
  Character_Player
  Character_Enemy_Common
  UI_Common
```

## 지금 프로젝트에 추천하는 구조

초기에는 다음 정도가 현실적입니다.

```text
Main Unity Project
  Assets/
    Game/
    Scripts/
    Scenes/
    UI/
    Addressables 설정

Submodule: ContentAssets
  Characters/
  Environments/
  Props/
  Audio/
  VFX/
  SharedMaterials/
```

그리고 Addressables group은 submodule의 폴더 구조를 그대로 따라가지 말고, 실제 게임 로딩 흐름 기준으로 설계합니다.

```text
Addressables Groups
  Common_Runtime
  Tutorial
  Stage_01
  Stage_02
  Character_Player
  Character_Enemy_Common
  UI_Common
```

## 지금 당장 피하면 좋은 뇌절

다음은 새 프로젝트 초기 단계에서는 보류하는 것이 좋습니다.

- 에셋 repo를 너무 잘게 여러 submodule로 쪼개기
- 모든 에셋을 Addressable로 만들기
- 모든 중복 dependency를 공통 group으로 몰아넣기
- remote catalog / CDN / load path branching을 지금부터 전부 설계하기
- submodule 경계와 Addressables group 경계를 강제로 일치시키기
- “나중에 DLC 할 수도 있으니까”라는 이유만으로 프로젝트를 과도하게 분리하기

특히 현재 조건이 “에셋을 외부로 노출하지 않음”이라면, remote catalog/CDN/load path branching은 당장 우선순위가 낮습니다.

## 세션에서 지금 참고할 부분 / 보류할 부분

### 지금 참고할 부분

- Addressables group을 같이 쓰이는 단위로 잡기
- 중복 dependency를 무조건 제거하지 않기
- remapper 메모리 문제를 고려해 불필요한 bundle 로드를 줄이기
- string path 대신 `AssetReferenceT`, `AssetLabelReference`를 사용해 실수를 줄이기
- Build Layout Report를 남겨 bundle 변경 원인을 추적할 수 있게 하기

### 지금 보류해도 되는 부분

- remote catalog 운영
- CDN bucket 설계
- append hash/no hash의 운영 배포 정책
- load path branching
- 국가/플랫폼/버전별 endpoint 분기
- DLC/확장팩용 별도 Unity project 분리

이 부분들은 실제로 외부 배포, 패치, 플랫폼별 다운로드, 원격 카탈로그 운영이 필요해졌을 때 다시 설계해도 늦지 않습니다.

- 출처: `2026-06-20_cecc5d`, transcript lines 110-154, 155-183, 223-296 — remote catalog, separate Unity projects, load path branching 설명

## 판단 기준 체크리스트

현재 구조를 유지할지 판단할 때는 아래 질문으로 보면 됩니다.

### Submodule을 유지할 이유가 있는가?

- 에셋 repo 접근 권한을 메인 코드 repo와 다르게 가져가야 하는가?
- 에셋 용량 때문에 메인 repo가 무거워지는가?
- 에셋 변경 주기와 코드 변경 주기가 꽤 다른가?
- 아트/콘텐츠 작업자가 메인 코드 repo 전체를 만질 필요가 없는가?
- 다른 프로젝트에서도 같은 에셋 묶음을 재사용할 가능성이 있는가?

여기에 2개 이상 해당하면 submodule 분리는 유지할 만합니다.

### Addressables group을 나눌 이유가 있는가?

- 특정 스테이지/모드에서만 쓰는 에셋이 많은가?
- 한번에 로드하면 메모리가 부담되는 큰 에셋 묶음이 있는가?
- 특정 콘텐츠만 업데이트하거나 교체할 가능성이 있는가?
- 로딩 시간/메모리 프로파일링에서 특정 bundle이 문제를 일으키는가?

여기에 해당하면 Addressables group을 런타임 기준으로 나누는 것이 좋습니다.

## 최종 권장안

현재는 다음 정도로 가는 것을 추천합니다.

1. 에셋 submodule은 유지한다.
2. submodule은 권한/용량/작업 분리용으로만 본다.
3. Addressables group은 submodule 폴더 구조와 별도로 설계한다.
4. group 기준은 “같이 로드되는가?”로 잡는다.
5. 작은 중복 dependency는 너무 강박적으로 제거하지 않는다.
6. remote catalog/CDN/load path branching은 지금은 보류한다.
7. 나중에 외부 배포/패치/플랫폼별 endpoint가 생기면 그때 세션의 후반부 내용을 다시 참고한다.

한 줄 요약:

> 지금 구조는 뇌절이라기보다 방향은 맞습니다. 다만 submodule은 소스 관리 경계, Addressables는 런타임 로딩 경계로 분리해서 생각하는 것이 핵심입니다.
