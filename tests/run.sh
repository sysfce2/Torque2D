#!/usr/bin/env bash
#
# macOS/Linux port of tests/run.ps1 -- runs the TorqueScript integration suites
# against the desktop debug build. Same idea as the PowerShell runner: each test
# is a boot script driving the real engine, so each gets its own process. The
# engine makes the boot script's own directory the working directory, so a stub
# is written at the repo root and the engine pointed at that; the stub execs the
# real test through tests/lib/prelude.cs.
#
# A test passes when it logs no FAIL lines and exits on its own.
#
# The input-driven suites -- the ones with a Win32-only *.input.ps1 companion
# posting real mouse/keyboard -- have no equivalent here, so they are skipped by
# default. Pass --with-input to run them anyway (they will get no synthetic input
# and will report their clicks as failures).
#
# Usage:
#   tests/run.sh                 every pass/fail suite
#   tests/run.sh colorPopup      one of them (glob allowed)
#   tests/run.sh --shots         the screenshot harnesses instead
#   tests/run.sh --list          what would run
#   tests/run.sh --timeout 120   seconds before a hung test is killed

# No `set -u`: macOS ships bash 3.2, where expanding an empty array (there may be
# no matching tests, or no extras beyond ORDER) is treated as an unbound variable.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT_DIR="$ROOT/tests"

EXE="$ROOT/Torque2D_DEBUG.app/Contents/MacOS/Torque2D_DEBUG"
if [[ ! -x "$EXE" ]]; then
    EXE="$ROOT/Torque2D_DEBUG"   # Linux drops the binary at the repo root
fi
LOG="$ROOT/console.log"
BOOT="$ROOT/_boot.cs"

NAME='*'
DIR='smoke'
LIST=0
TIMEOUT=90
WITH_INPUT=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --shots)      DIR='shots' ;;
        --list)       LIST=1 ;;
        --with-input) WITH_INPUT=1 ;;
        --timeout)    TIMEOUT="$2"; shift ;;
        --*)          echo "unknown option: $1" >&2; exit 2 ;;
        *)            NAME="$1" ;;
    esac
    shift
done

if [[ ! -x "$EXE" ]]; then
    echo "No Torque2D_DEBUG binary found - build it first:" >&2
    echo "  cmake --build build/macos --target Torque2D" >&2
    exit 2
fi

# bitmapPathRead boots fresh to read back what bitmapPathWrite saved, so it must
# keep the previous test's project folder rather than starting clean.
KEEP_PROJECT=(bitmapPathRead)

# Order matters for one pair only: bitmapPathWrite before bitmapPathRead. The
# input-driven suites are listed so ordering holds when --with-input is passed.
ORDER=(profileEditor profileForm border borderPane standalone \
       headerPane colorPopup themeApply font assetPicker \
       tooltipProfile textClick bitmapPathWrite bitmapPathRead \
       toybox planetX)

# Tests whose input path is Win32-only; skipped unless --with-input. Derived from
# the companion scripts actually on disk rather than named here, because a
# hardcoded list goes stale the moment a suite gains one -- which it had: six
# input-driven suites were running without a driver and reporting their clicks as
# engine failures.
INPUT_ONLY=()
for f in "$SCRIPT_DIR/smoke"/*.input.ps1; do
    [[ -e "$f" ]] || continue
    INPUT_ONLY+=("$(basename "$f" .input.ps1)")
done

contains() { local n="$1"; shift; for e in "$@"; do [[ "$e" == "$n" ]] && return 0; done; return 1; }

# Discover the suites present, ordered by ORDER then alphabetically for the rest.
found=()
for f in "$SCRIPT_DIR/$DIR"/*.cs; do
    [[ -e "$f" ]] || continue
    b="$(basename "$f" .cs)"
    found+=("$b")
done

ordered=()
for t in "${ORDER[@]}"; do
    contains "$t" "${found[@]}" && ordered+=("$t")
done
extras=()
for t in "${found[@]}"; do
    contains "$t" "${ORDER[@]}" || extras+=("$t")
done
IFS=$'\n' extras=($(printf '%s\n' "${extras[@]:-}" | sort)); unset IFS

tests=()
for t in "${ordered[@]}" "${extras[@]}"; do
    [[ -z "$t" ]] && continue
    # shellcheck disable=SC2053
    [[ "$t" == $NAME ]] || continue
    if [[ $WITH_INPUT -eq 0 && "$DIR" == "smoke" ]] && contains "$t" "${INPUT_ONLY[@]}"; then
        continue
    fi
    tests+=("$t")
done

if [[ ${#tests[@]} -eq 0 ]]; then
    echo "No tests in tests/$DIR matching '$NAME'."
    exit 2
fi

if [[ $LIST -eq 1 ]]; then
    printf '  %s\n' "${tests[@]}"
    exit 0
fi

echo
echo "Running ${#tests[@]} $DIR test(s) from tests/$DIR"
echo

bad=()
for test in "${tests[@]}"; do
    script="tests/$DIR/$test.cs"
    printf '  %-18s ' "$test"

    rm -f "$LOG"

    # Start from a clean throwaway project folder, exactly as run.ps1 does: only
    # the *SmokeProject/*ShotProject/smokeThemeProject folders a test builds for
    # itself, never PlanetX or toybox, which are real content.
    if ! contains "$test" "${KEEP_PROJECT[@]}"; then
        while IFS= read -r folder; do
            if [[ "$folder" =~ (SmokeProject|ShotProject)$ || "$folder" == "smokeThemeProject" ]]; then
                rm -rf "${ROOT:?}/$folder"
            fi
        done < <(grep -oE 'setProjectFolder\("[^"]+"' "$SCRIPT_DIR/$DIR/$test.cs" | sed -E 's/.*"([^"]+)"/\1/')
    fi

    # The stub sits at the repo root so the engine's working directory becomes the
    # root; only here is a plain "./" the repo root.
    {
        echo '// Generated by tests/run.sh. Not tracked; safe to delete.'
        echo 'exec("./tests/lib/prelude.cs");'
        echo "exec(\"./$script\");"
    } > "$BOOT"

    started=$(date +%s)

    "$EXE" _boot.cs >/dev/null 2>&1 &
    pid=$!

    # Wait up to TIMEOUT for the process to exit on its own.
    elapsed=0
    hung=1
    while [[ $elapsed -lt $TIMEOUT ]]; do
        if ! kill -0 "$pid" 2>/dev/null; then hung=0; break; fi
        sleep 1
        elapsed=$((elapsed + 1))
    done
    if [[ $hung -eq 1 ]]; then
        kill -9 "$pid" 2>/dev/null
        sleep 1
    fi
    wait "$pid" 2>/dev/null

    pass=0; fail=0
    if [[ -f "$LOG" ]]; then
        # grep -c prints 0 on no match (and exits 1); take its stdout regardless.
        pass=$(grep -c 'PASS:' "$LOG" 2>/dev/null); pass=${pass:-0}
        fail=$(grep -c 'FAIL:' "$LOG" 2>/dev/null); fail=${fail:-0}
    fi
    wrote=0
    if [[ -d "$ROOT/shots" ]]; then
        wrote=$(find "$ROOT/shots" -type f -newermt "@$started" 2>/dev/null | wc -l | tr -d ' ')
    fi

    ok=1
    note=''
    if [[ "$DIR" == "shots" ]]; then
        { [[ $wrote -gt 0 && $hung -eq 0 ]]; } || ok=0
        summary="$wrote shot(s)"
    else
        { [[ $fail -eq 0 && $hung -eq 0 ]]; } || ok=0
        summary="$pass passed"
    fi
    [[ $hung -eq 1 ]] && note="KILLED after ${TIMEOUT}s"
    [[ $fail -gt 0 ]] && note="$fail FAILED${note:+, $note}"

    if [[ $ok -eq 1 ]]; then
        printf '%-14s %s\n' "$summary" "$note"
    else
        printf '%-14s %s\n' "$summary" "$note"
        grep 'FAIL:' "$LOG" 2>/dev/null | head -6 | sed 's/^/                     /'
        if [[ $hung -eq 1 ]]; then
            last=$(grep -v '^[[:space:]]*$' "$LOG" 2>/dev/null | tail -1)
            [[ -n "$last" ]] && echo "                     last log line: $last"
        fi
        bad+=("$test")
    fi
done

rm -f "$BOOT"

echo
if [[ ${#bad[@]} -gt 0 ]]; then
    echo "${#bad[@]} of ${#tests[@]} not as expected: ${bad[*]}"
    exit 1
else
    echo "All ${#tests[@]} as expected."
    exit 0
fi
