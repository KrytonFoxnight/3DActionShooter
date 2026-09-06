# 에셋 저장소(ThirdParty 서브모듈) 관리 가이드

`Assets/ThirdParty`는 별도 git 저장소(`KrytonFoxnight/3DActionShooter-Assets`)를 서브모듈로 물린 것이다.
이 문서는 그 관리 원칙·일상 루틴·사고 대응 절차를 기록한다. (2026-07-19 정리)

## 1. 구조 원리 — "두 저장소, 하나의 진실"

- 부모 저장소는 서브모듈의 **내용을 추적하지 않는다.** 각 커밋에 "이 코드에는 에셋 repo의 이 커밋"이라는 **포인터(커밋 해시) 한 줄**만 기록한다.
- 각 체크아웃(본진, wt1, wt2, Mac)의 ThirdParty 폴더는 그 포인터에서 **파생된 복사본**이다. 복사본끼리는 동기화하지 않으며, 모든 전파는 포인터를 통해서만 일어난다.
- 따라서 브랜치마다 에셋 버전이 달라도 정상이고, 워크트리마다 별도 클론(~300MB)인 것도 정상이다. **폴더 공유(junction)는 금지** — 한쪽이 항상 틀린 에셋을 보게 된다.

## 2. 적용된 git 설정

부모 repo의 `.git/config`에 저장되어 **같은 머신의 모든 워크트리가 공유**한다.

| 설정 | 효과 |
|---|---|
| `submodule.recurse true` | `switch`/`pull` 시 서브모듈 자동 체크아웃 |
| `status.submodulesummary 1` | status에 서브모듈 커밋 변화 요약 표시 |
| `diff.submodule log` | diff에 해시 대신 커밋 목록 표시 |
| `push.recurseSubmodules on-demand` | 부모 push 시 필요한 서브모듈 커밋을 먼저 자동 push (포인터 미아 방지) |

⚠️ **이 설정은 머신 로컬이다.** 새 머신(Mac 등)에서 clone 하면 같은 4줄을 다시 걸어야 한다.

## 3. 일상 루틴

1. **평상시**: 할 일 없음. 브랜치 전환/pull/push 동기화는 위 설정이 전자동 처리한다.
2. **에셋을 의도적으로 고칠 때**: 고치기 전에 **어느 체크아웃에서 고칠지 하나만 정한다.** 순서는 항상
   `서브모듈 안에서 커밋·push → 부모에서 chore 포인터 커밋`. 다른 체크아웃은 pull 때 자동으로 따라온다.
3. **`git status`에 ` m Assets/ThirdParty`가 보일 때**: 아래 4장 절차로 그 자리에서 정리한다. 별도 순찰은 불필요 — 커밋 전 status 확인으로 충분하다.

## 4. ` m` 램프 대응 절차

` m`(소문자) = 서브모듈 안에 미커밋 변경이 있다는 표시. 부모 커밋에 절대 실리지 않는 유령 상태이므로 방치하지 않는다.
Unity는 에셋을 만지면 서브모듈 안에 자동으로 쓴다(메타 업그레이드, 임포트 설정, 프리팹 수정). 램프는 그걸 잡는 유일한 창구다.

```powershell
# 1) 내용 확인
git -C Assets/ThirdParty status
git -C Assets/ThirdParty diff
```

그 다음 질문 하나로 분기한다: **"이 변경의 정본이 어디 있는가?"**

| 정본 위치 | 처리 |
|---|---|
| 이미 원격에 커밋되어 있음 | `git -C Assets/ThirdParty restore .` → `fetch` → `checkout <정본 해시>` |
| 지금 이 로컬 수정이 정본 | 서브모듈 안에서 `add` → `commit` → `push` |
| 실험/쓰레기 | `git -C Assets/ThirdParty restore .` |

세 경우 모두 마무리는 동일:

```powershell
git add Assets/ThirdParty
git commit -m "chore: ThirdParty 서브모듈 ... 반영"
git status   # m 이 사라졌으면 종료
```

사고 사례(2026-07-19): 같은 콜라이더 수정이 wt2 작업 폴더(미커밋)와 에셋 repo(커밋)에 서로 다른 판본으로 공존
→ 로컬 판본 폐기 후 정본 체크아웃으로 해소. 원인은 같은 수정을 두 곳에서 한 것 — 루틴 3-2를 지키면 재발하지 않는다.

## 5. 새 워크트리(wtN) 생성 시

```powershell
git worktree add <본진 경로>-wtN <브랜치>
cd <본진 경로>-wtN
git submodule update --init --reference <본진 경로>/Assets/ThirdParty
```

- `git worktree add`는 서브모듈을 자동 클론하지 않는다. 첫 init은 워크트리당 1회 수동 필수.
- `--reference`는 본진의 객체를 재사용해 네트워크/디스크를 아끼는 선택 옵션 (본진 폴더가 존재하는 한 안전).

## 6. 협업 확장 시 한계 (미리 아는 부채)

현 구조는 **1인 + Claude 병렬 세션** 환경에 최적화된 것이다. 참여자 전원이 같은 머신·설정·규칙을 공유하므로 서브모듈의 알려진 협업 사고(포인터 미아, 포인터 역행 커밋, 초기화 누락, 로컬 설정 미전파)가 성립하지 않는다.

실제 팀 협업으로 확장할 경우 서브모듈은 업계 표준이 아니며, 다음으로 이행을 검토한다:

- **모노레포 + Git LFS** — 에셋을 본 repo에 합치고 대용량만 LFS로
- **Perforce / Unity DevOps(Plastic)** — 아티스트 친화적 잠금 모델, 대형 게임사 표준
- 서브모듈을 유지해야 한다면: CI에서 포인터 역행 감지 + 온보딩 스크립트(`clone --recurse-submodules` + 설정 자동 설치)가 필수 세트다.

판단 기준: 팀원이 생기는 시점 = 이행 검토 시점. 그 전까지는 현 구조를 유지한다.
