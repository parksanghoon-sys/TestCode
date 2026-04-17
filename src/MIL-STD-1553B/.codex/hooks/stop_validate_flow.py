#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
MIL-STD-1553B 하네스 엔지니어링 저장소용 Stop 훅이다.

역할:
- 코드 변경이 발생했을 때 설계 검토 문서와 테스트 변경 여부를 점검한다.
- 규칙을 위반하면 Codex에게 한 번 더 계속 진행하도록 피드백을 반환한다.
"""

import json
import os
import pathlib
import subprocess
import sys
from typing import Iterable, List


CODE_EXTENSIONS = {
    ".cs",
    ".cpp",
    ".cxx",
    ".cc",
    ".c",
    ".h",
    ".hpp",
    ".ixx",
}

DESIGN_PREFIXES = (
    "docs/design/",
)

TEST_PREFIXES = (
    "tests/",
    "docs/test/",
)


def _read_input() -> dict:
    """<summary>표준 입력으로 전달된 훅 JSON을 읽습니다.</summary>"""
    raw = sys.stdin.read().strip()
    if not raw:
        return {}
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return {}


def _run_git(cwd: pathlib.Path, *args: str) -> str:
    """<summary>git 명령을 실행하고 표준 출력을 문자열로 반환합니다.</summary>"""
    result = subprocess.run(
        ["git", *args],
        cwd=str(cwd),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        return ""
    return result.stdout.strip()


def _resolve_repo_root(cwd: pathlib.Path) -> pathlib.Path:
    """<summary>현재 작업 디렉터리에서 git 루트를 찾습니다.</summary>"""
    root = _run_git(cwd, "rev-parse", "--show-toplevel")
    if root:
        return pathlib.Path(root)
    return cwd


def _list_changed_files(repo_root: pathlib.Path) -> List[str]:
    """<summary>현재 작업 트리 기준 변경 파일 목록을 반환합니다.</summary>"""
    # HEAD가 없는 초기 저장소도 고려한다.
    has_head = _run_git(repo_root, "rev-parse", "--verify", "HEAD")
    if has_head:
        diff_output = _run_git(repo_root, "diff", "--name-only", "HEAD", "--")
        return [line.strip() for line in diff_output.splitlines() if line.strip()]

    status_output = _run_git(repo_root, "status", "--porcelain")
    changed = []
    for line in status_output.splitlines():
        if not line.strip():
            continue
        path = line[3:].strip()
        if path:
            changed.append(path)
    return changed


def _has_code_change(paths: Iterable[str]) -> bool:
    """<summary>코드 파일 변경이 있는지 확인합니다.</summary>"""
    for path in paths:
        extension = pathlib.Path(path).suffix.lower()
        if extension in CODE_EXTENSIONS:
            return True
    return False


def _has_prefix(paths: Iterable[str], prefixes: Iterable[str]) -> bool:
    """<summary>특정 접두 경로 변경이 있는지 확인합니다.</summary>"""
    normalized = [path.replace("\\", "/") for path in paths]
    for path in normalized:
        for prefix in prefixes:
            if path.startswith(prefix):
                return True
    return False


def _build_reason(missing_design: bool, missing_test: bool) -> str:
    """<summary>계속 진행 메시지를 생성합니다.</summary>"""
    reasons = []
    if missing_design:
        reasons.append(
            "코드 변경이 감지되었지만 docs/design 아래 설계 검토 문서 변경이 없습니다. "
            "설계 검토 → 구현 → 테스트 순서를 지키기 위해 설계 문서를 먼저 작성하거나 갱신하세요."
        )
    if missing_test:
        reasons.append(
            "코드 변경이 감지되었지만 tests 또는 docs/test 아래 테스트 관련 변경이 없습니다. "
            "TDD 3색 원칙에 따라 실패 테스트, 최소 구현, 리팩터링 근거를 남기고 테스트를 보강하세요."
        )
    return " ".join(reasons)


def main() -> int:
    """<summary>훅 진입점입니다.</summary>"""
    payload = _read_input()
    cwd_text = payload.get("cwd") or os.getcwd()
    cwd = pathlib.Path(cwd_text)
    repo_root = _resolve_repo_root(cwd)
    changed_files = _list_changed_files(repo_root)

    if not changed_files:
        print(json.dumps({"continue": True}, ensure_ascii=False))
        return 0

    if not _has_code_change(changed_files):
        print(json.dumps({"continue": True}, ensure_ascii=False))
        return 0

    missing_design = not _has_prefix(changed_files, DESIGN_PREFIXES)
    missing_test = not _has_prefix(changed_files, TEST_PREFIXES)

    if missing_design or missing_test:
        reason = _build_reason(missing_design, missing_test)
        print(
            json.dumps(
                {
                    "decision": "block",
                    "reason": reason,
                },
                ensure_ascii=False,
            )
        )
        return 0

    print(json.dumps({"continue": True}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
