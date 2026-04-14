# Hook 유틸리티와 현재 활성 워크플로

이 폴더는 이제 WPF dev pack의 실제 훅 실행기까지 포함합니다.

## 현재 활성 경로

실제 Git hook 워크플로는 아래로 연결됩니다.

- `../../.githooks/pre-commit`
- `../../.githooks/pre-push`
- `../scripts/Install-GitHooks.ps1`
- `../scripts/Invoke-CodexPreCommit.ps1`
- `../scripts/Invoke-CodexPrePush.ps1`
- `../scripts/codex-hook.config.json`
- `./WpfDevPack.HookRunner/WpfDevPack.HookRunner.csproj`

클론마다 한 번 설치:

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

## 이제 자동 실행되는 항목

Git hook은 이제 HookRunner 프로젝트를 호출하고, 이 실행기가 이 폴더의 helper 유틸 목적을 Git hook 파이프라인에 맞게 수행합니다.

`pre-commit`에서 사용:

- `CodeFormatter.cs` 의도 -> `dotnet format` 기반 C# 포맷팅
- `XamlValidator.cs` 의도 -> XAML/XML 검증 및 경고
- `MvvmViolationDetector.cs` 의도 -> ViewModel 계층 위반 경고
- `WpfKeywordDetector.cs` 의도 -> 관련 `.agents/skills/*` 추천
- `McpDependencyChecker.cs` 의도 -> 권장 MCP 참조 누락 경고

`pre-push`에서 사용:

- `BuildErrorDiagnoser.cs` 의도 -> 빌드/테스트 실패 시 진단 가이드 메시지
- `McpDependencyChecker.cs` 의도 -> 권장 MCP 참조 누락 경고

여전히 참고용만 유지:

- `HandMirrorReminder.cs` 는 Codex/MCP 질의 가이드 성격이라 Git 훅 자동 실행에는 맞지 않아 참고용으로만 둡니다.

## 수동 실행

직접 실행도 가능합니다.

```powershell
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1
```
