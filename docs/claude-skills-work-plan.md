# 작업 지시서 — Claude Skill 관리 체계 재구축 (chore/claude-skills)

> 본진 세션(2026-07-19)에서 논의·확정된 내용의 인수인계 문서. wt1(`E:\Unity\3DActionShooter-wt1`, 브랜치 `chore/claude-skills`) 세션이 이 문서를 기준으로 작업한다.

## 1. 배경

- 사용자는 회사에서 자체 스킬 관리 체계를 운영했음: `Tools/claude-skills/*/skill.md` (frontmatter `name`/`version` 계약) + `install_claude_skills.sh` (semver 비교 설치 → `.claude/commands/`).
- 이 체계는 **견본으로만** 현재 repo의 `Tools/`에 들어와 있음. 그대로 재사용하지 않는다.
- 평가 결론: 인프라는 잘 만든 임시 표준이었으나, 배포 범위가 repo 하나였고 설치 대상(`.claude/commands/`)이 구식. 공식 형식으로 이전하되 배포는 자체 스크립트로 한다(아래 결정사항).

## 2. 확정된 결정사항 (재논의 불필요)

1. **중앙 repo 하나** — marketplace 겸 플러그인 저장소. private. 모든 스킬의 SSOT.
2. **공식 marketplace 형식을 스키마로 채택**: `.claude-plugin/marketplace.json` + 플러그인별 `.claude-plugin/plugin.json`(semver) + `skills/<name>/SKILL.md`.
3. **실제 배포·설치는 Python 스크립트(`deploy.py`)로 통일.** Claude Code 공식 `/plugin` 설치 채널은 쓰지 않는다(채널 중복 금지). 단, repo가 공식 형식을 지키므로 언제든 공식 채널로 전환 가능 — 이 호환성은 비상구로 유지.
4. **모듈형 소비**: 각 프로젝트는 매니페스트로 필요한 플러그인만 선택 설치. 플러그인 묶음 기준은 "항상 같이 쓰는 것끼리만".
5. **제3 서비스(Codex/Copilot/Gemini 등) 호환을 설계 목표에 포함.** SKILL.md(Agent Skills)는 공개 표준이라 알맹이는 이식됨. 이식성 경계 규칙:
   - 스킬 폴더는 자기완결 (참조 스크립트·데이터는 폴더 안, 상대 경로)
   - Claude Code 전용 지식(plugin.json 등)은 껍데기에만, SKILL.md 본문에 넣지 않음
6. **워크플로 구조**: 본진은 사용자 개인 전용, Claude 세션은 wt1/wt2에서만 작업.

## 3. 작업 범위 — 두 갈래로 나뉨 (혼동 주의)

### A. 새 marketplace repo (Unity repo **밖**, 별도 위치에 생성)

핵심 산출물. 위치·이름은 사용자에게 확인 (미확정 — §6).

```text
<marketplace-repo>/
├── .claude-plugin/marketplace.json      # 플러그인 목록, source는 상대경로 "./..."
├── <plugin-A>/
│   ├── .claude-plugin/plugin.json       # name, version(semver), description
│   └── skills/<skill-name>/
│       ├── SKILL.md                     # 대문자. Agent Skills 표준 준수
│       └── scripts/                     # 스킬과 동봉, 상대 경로 참조
├── deploy.py                            # 배포기 (아래 §4)
└── README.md                            # 운영 규율 (버저닝 규칙 등 — Tools/claude-skills/CLAUDE.md의 규율 이사)
```

### B. Unity repo의 chore/claude-skills 브랜치 (이 워크트리)

- `Tools/claude-skills/` 견본 처리: **회사 내부 정보 포함(프로젝트 ID, DB 스키마, 사례 코드) — 이 repo에 커밋되지 않게 확정할 것.** 현재 git 추적 여부부터 확인. 추적 안 되고 있으면 `.gitignore`에 명시 추가, 추적 중이면 제거.
- 이 프로젝트가 소비자가 되는 설정(매니페스트 등)은 marketplace repo가 생긴 뒤에.
- 기존 `.claude/skills/guide/`(라인 단위 가이드 스킬)는 이 프로젝트 전용이므로 그대로 둠. 범용화하고 싶으면 marketplace로 이관은 선택.
  - **(2026-07-19 결정 변경)** 사용자 확정: 프로젝트 전용 여부와 무관하게 **모든 자작 스킬의 정본은 중앙 repo**. guide는 marketplace의 `guide` 플러그인 1.0.0으로 이관 완료, 이 repo는 `skills-manifest.json`으로 소비.

## 4. deploy.py 설계 (합의된 청사진)

4요소:

1. **소스 리더** — marketplace.json → 각 plugin.json → 스킬 폴더 열거. 공식 스키마 그대로 파싱.
2. **프로젝트 매니페스트** — 소비 프로젝트마다 `skills-manifest.json`: 어느 플러그인을 어느 도구(claude-code, codex, ...)에 설치할지 선언.
3. **어댑터** — 도구별 클래스. 대상 경로 + 도구 특이사항만 담당. 얇게 유지.
   - claude-code 어댑터: `.claude/skills/<name>/`로 **폴더째**(scripts 포함) 복사.
   - 제3 도구 어댑터: 각 도구의 스킬 탐색 경로는 **구현 시점에 현행 문서로 확인** (버전 따라 바뀜. 단정 금지).
4. **락파일** — 설치 대상마다 `installed.lock.json`(설치된 버전 기록). 이 기준으로:
   - semver 비교 (손파싱 금지 — `packaging.version` 사용)
   - 다운그레이드 보호 (로컬이 높으면 skip — 구 bash 체계의 좋은 기능 계승)
   - **prune** (매니페스트에서 빠진 스킬 제거 — 구 체계에 없던 기능)
   - 설치 로그

추가 요구사항:
- **버전 규율 기계 검사**: 소스 내용 해시가 락파일과 다른데 version이 같으면 경고. (구 체계의 "[OK] 오판으로 변경분 누락" 함정 방지)
- 비정상 버전 문자열은 **조용히 넘기지 말고 에러** (구 bash의 침묵 실패 재발 금지)
- 구현 시작 전 기존 오픈소스(크로스벤더 스킬 설치기) 존재 여부 조사 먼저. 있으면 채택 검토.

## 5. 스킬 마이그레이션 규칙 (견본 → 새 형식으로 옮길 때)

| 항목 | 처리 |
|---|---|
| 파일명 `skill.md` | → `SKILL.md` (대문자) |
| frontmatter `name` | 유지 (기본값은 디렉토리명) |
| frontmatter `description` | 유지 — 자동 호출 트리거 문구 포함 유지 |
| frontmatter `version` | plugin.json으로 이동. 스킬에 문서용으로 남겨도 무해 |
| frontmatter `user_invocable` | 삭제 (스킬은 기본 사용자 호출 가능. 반대 개념만 존재: `disable-model-invocation`) |
| frontmatter `argument-hint` | 삭제, 필요하면 description에 사용례로 병합 |
| 스크립트 경로 `Tools/claude-skills/...` | → 스킬 폴더 기준 상대 경로 |

**하드코딩 제거 (필수):**
- 개인 서비스 계정 키 파일명(`...-898feb18c2.json` 류) → glob 패턴 또는 환경변수로
- Mac 절대 경로(`/Users/...`) 제거
- `/tmp` venv 경로 → OS 중립적으로 (Windows 병행 사용 전제)

**주의: 견본 스킬 13종은 회사 자산·회사 정보 포함.** 어떤 스킬을 실제로 개인 marketplace에 이식할지는 사용자 판단 사항 (§6). 이식하더라도 회사 식별 정보(프로젝트 ID, 스키마, 사례)는 제거·일반화 필수.

## 6. 사용자에게 확인할 것 (작업 시작 시 질문)

1. 새 marketplace repo의 **위치와 이름** (예: `E:\Repos\claude-plugins`?)
2. **플러그인 묶음 단위** — 잘게(스킬≈플러그인) vs 도메인 묶음. 기준은 "항상 같이 쓰는 것끼리". 애매하면 잘게.
3. 견본 13종 중 **실제 이식 대상** — 회사 정보 일반화를 감수하고 가져갈 것 vs 구조만 참고하고 새로 쓸 것.
4. deploy.py가 지원할 **제3 도구 목록** (Codex? Copilot? Gemini CLI?) — 어댑터 조사 범위 결정용.

## 7. 참고 자료

- 공식 문서: [Plugins](https://code.claude.com/docs/en/plugins.md) · [Plugin Marketplaces](https://code.claude.com/docs/en/plugin-marketplaces.md) · [Plugins Reference](https://code.claude.com/docs/en/plugins-reference.md) · [Skills](https://code.claude.com/docs/en/skills.md) · [Settings](https://code.claude.com/docs/en/settings.md)
- Agent Skills 공개 표준: [agentskills.io](https://agentskills.io/home) · [Anthropic 엔지니어링 글](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills)
- 로컬 개발 반복: `claude --plugin-dir <플러그인경로>` + `/reload-plugins`
- 견본(구 체계) 위치: `Tools/claude-skills/` + `Tools/install_claude_skills.sh` — 계승할 장점: frontmatter 계약 검사, semver 정책 문서, 다운그레이드 보호, 설치 로그
