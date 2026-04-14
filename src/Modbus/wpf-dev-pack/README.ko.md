[🇺🇸 English](./README.md)

# wpf-dev-pack

**OpenAI Codex**용 WPF 개발 팩입니다.

## 포함 내용

- WPF 전용 프로젝트 지침: `AGENTS.md`
- 재사용 가능한 WPF 워크플로: `.agents/skills/`
- 선택적 프로젝트 전용 커스텀 에이전트: `.codex/agents/`
- 재사용 규칙과 패턴: `rules/`
- 선택형 보조 유틸: `hooks/`
- 실제 활성 로컬 Git 훅 워크플로: `scripts/`

## 요구사항

- Codex CLI 또는 Codex 지원 클라이언트
- 로컬 보조 유틸리티 실행용 .NET SDK
- 프레임워크 문서 조회, 코드 검색, C# 인텔리전스용 선택적 MCP 서버

## Codex 설치

```bash
npm i -g @openai/codex
codex
```

## 사용 방법

- `wpf-dev-pack/`를 Codex에서 엽니다.
- `AGENTS.md`를 먼저 읽게 합니다.
- 재사용 워크플로는 `.agents/skills/` 아래에 둡니다.
- 반복적으로 맡길 전문 역할이 필요하면 `.codex/agents/`를 사용합니다.

## 자주 쓰는 스킬

- `make-wpf-project`
- `make-wpf-custom-control`
- `make-wpf-usercontrol`
- `make-wpf-viewmodel`
- `implementing-communitytoolkit-mvvm`
- `customizing-controltemplate`
- `rendering-with-drawingcontext`
- `rendering-wpf-high-performance`


## 로컬 Git 훅 워크플로

클론마다 한 번 설치합니다.

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

연결되는 경로:

- `.githooks/pre-commit` → `scripts/Invoke-CodexPreCommit.ps1`
- `.githooks/pre-push` → `scripts/Invoke-CodexPrePush.ps1`

`hooks/`에는 선택형 보조 유틸이 남아 있고, 실제 활성 훅 경로는 `scripts/`입니다.


## Git hook 워크플로

`pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1` 로 훅을 설치합니다. 현재 활성 경로는 `.githooks/*`, PowerShell 진입 스크립트, `wpf-dev-pack/hooks/WpfDevPack.HookRunner/` 를 사용하며 `wpf-dev-pack/hooks/*.cs` helper 목적을 실제 훅 검사에 반영합니다.
