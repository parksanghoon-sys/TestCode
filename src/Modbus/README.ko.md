[🇺🇸 English](./README.md)

# dotnet-with-codex

**OpenAI Codex**용 .NET / WPF 템플릿 저장소입니다.

## 개요

이 저장소는 Codex 중심 워크플로에 맞춰 정리되어 있습니다.

- 프로젝트 지침: `AGENTS.md`
- 재사용 저장소 스킬: `.agents/skills/`
- 프로젝트별 Codex 설정: `.codex/config.toml`
- WPF 특화 작업 공간: `wpf-dev-pack/`

## 저장소 구성

### [wpf-dev-pack](./wpf-dev-pack)

프로젝트 생성, MVVM, 스타일링, 렌더링, 리뷰 작업에 맞춘 WPF 전용 Codex 작업 공간입니다.

## 요구사항

- Codex CLI 또는 Codex 지원 클라이언트
- `wpf-dev-pack/scripts`와 `wpf-dev-pack/hooks`의 로컬 스크립트 실행용 .NET SDK / PowerShell
- 문서 조회, 시맨틱 코드 검색, C# 도구 연동용 선택적 MCP 서버

## Codex 설치

```bash
npm i -g @openai/codex
codex
```

## 권장 사용 순서

1. 저장소를 Codex에서 엽니다.
2. 가장 가까운 `AGENTS.md`를 먼저 읽게 합니다.
3. 공용 저장소 스킬은 `.agents/skills/`에 유지합니다.
4. WPF 특화 작업이 필요하면 `wpf-dev-pack/` 아래에서 작업합니다.
5. 프로젝트별 MCP 서버는 `.codex/config.toml` 또는 `codex mcp add`로 추가합니다.

## Hook 설정

클론마다 한 번만 로컬 Git 훅을 설치합니다.

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

연결되는 경로:

- `.githooks/pre-commit` → `wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1`
- `.githooks/pre-push` → `wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1`
- 동작 설정 → `wpf-dev-pack/scripts/codex-hook.config.json`

`pre-commit`은 스테이징된 C# 포맷과 XAML 기본 검증을 수행하고, `pre-push`는 빌드와 테스트 프로젝트 실행을 수행합니다.

## 참고

- 이 정리본에서는 레거시 플러그인 관련 파일과 깨진 버전/릴리스 훅을 제거했습니다.
- `wpf-dev-pack/hooks/`는 선택형 보조 유틸이고, 실제 활성 훅 워크플로는 `wpf-dev-pack/scripts/`와 `.githooks/`를 사용합니다.

## 라이선스

MIT.


## Git hook 워크플로

`pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1` 로 훅을 설치합니다. 현재 활성 경로는 `.githooks/*`, PowerShell 진입 스크립트, `wpf-dev-pack/hooks/WpfDevPack.HookRunner/` 를 사용하며 `wpf-dev-pack/hooks/*.cs` helper 목적을 실제 훅 검사에 반영합니다.
