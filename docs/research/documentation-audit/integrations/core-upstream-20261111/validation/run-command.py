"""Exclusive coordinator command capture; retain actual toolchain text before rotation.

This tool executes only when the coordinator invokes it after releasing the checkout.
Every attempt gets a new directory. Failures retain their original stdout/stderr.
"""
import argparse
import datetime
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import shlex
import signal
import shutil
import subprocess
import sys
import threading

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]


def require(value, message):
    if not value:
        raise ValueError(message)


def digest(path):
    raw = path.read_bytes()
    return dict(path=str(path), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())


def check_result(result, exit_code, targets, projects):
    require(exit_code == 0 and result["status"] == "passed", "Command/result reports failure")
    require(result["warningCount"] == 0 and not result["warnings"], "Warnings require coordinator review")
    actual_targets = [row["name"] for row in result["targets"]]
    require(len(actual_targets) == len(set(actual_targets)), "Duplicate result targets")
    require(set(targets) <= set(actual_targets), "Requested target missing from actual result")
    require(all(row["status"] == "passed" and not row["error"] for row in result["targets"]), "Actual target failed")
    actual_projects = [(row["target"], row["project"]) for row in result["projects"]]
    require(len(actual_projects) == len(set(actual_projects)), "Duplicate project result")
    expected_projects = [tuple(row.split(":", 1)) for row in projects]
    require(set(actual_projects) == set(expected_projects), "Actual target:project set differs from explicit expectation")
    for row in result["projects"]:
        require(row["outcome"] == "passed" and not row["failedTests"], "Project failed or executed no tests")
        require(type(row["executed"]) is int and row["executed"] > 0 and row["failed"] == 0,
                "Missing/zero execution or nonzero failure count")
        require(type(row["skipped"]) is int and row["skipped"] >= 0, "Missing skipped count")
    return dict(targets=actual_targets, projects=len(actual_projects),
                executed=sum(row["executed"] for row in result["projects"]),
                skipped=sum(row["skipped"] for row in result["projects"]), failed=0, warnings=0)


def active_guard(root):
    marker = root / "artifacts/toolchain/active-run.txt"
    if marker.exists():
        pid = int(marker.read_text().split("\t", 1)[0])
        try:
            os.kill(pid, 0)
        except ProcessLookupError:
            return
        raise ValueError(f"Toolchain process {pid} is active; coordinator retains checkout ownership")


def retain_text(source, target):
    require(not source.is_symlink() and source.is_file(), f"Require regular text input: {source}")
    raw = source.read_bytes()
    raw.decode("utf-8-sig")
    require(b"\0" not in raw and not raw.startswith((b"MZ", b"\x7fELF")), "Retention accepts text only")
    target.parent.mkdir(parents=True, exist_ok=True)
    require(not target.exists(), f"Preserve existing retention: {target}")
    shutil.copyfile(source, target)
    require(target.read_bytes() == raw, "Retention byte mismatch")
    return digest(target)


def capture(args):
    command = args.command
    if command[:1] == ["--"]:
        command = command[1:]
    require(command, "Supply a command after --")
    root = args.cwd.resolve()
    require(root == ROOT.resolve(), "Use this packet's integration checkout")
    require(re.fullmatch(r"[a-z0-9][a-z0-9-]*", args.label), "Use a simple unique attempt label")
    if args.expect_projects_file:
        require(args.toolchain, "Project expectation file requires --toolchain")
        expected = json.loads(args.expect_projects_file.read_bytes())
        require(isinstance(expected, list) and all(isinstance(row, str) for row in expected), "Expected a JSON string array")
        args.expect_project.extend(expected)
    require(len(args.expect_project) == len(set(args.expect_project)), "Duplicate expected projects")
    if args.toolchain:
        require(command[:2] == ["dotnet", "toolchain.cs"] and args.expect_target,
                "Toolchain capture requires dotnet toolchain.cs and explicit expected targets")
    else:
        require(not args.expect_target and not args.expect_project, "Result expectations require --toolchain")
    with (HERE / "coordinator-command.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        active_guard(root)
        destination = HERE / "commands" / args.label
        destination.mkdir(parents=True, exist_ok=False)
        runs = root / "artifacts/toolchain/runs"
        previous = {p.name for p in runs.iterdir()} if runs.exists() else set()
        receipt = dict(command=command, cwd=str(root), started=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                       logger=digest(Path(__file__).resolve()), expected_targets=args.expect_target,
                       expected_projects=args.expect_project, status="started")
        (destination / "invocation.json").write_text(json.dumps(receipt, indent=2) + "\n")
        code = None
        problem = None
        try:
            with (destination / "stdout.log").open("xb") as stdout, (destination / "stderr.log").open("xb") as stderr:
                child = subprocess.Popen(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
                stream_errors = []

                def pump(source, output, console):
                    try:
                        while block := source.read1(65536):
                            output.write(block)
                            output.flush()
                            try:
                                console.buffer.write(block)
                                console.flush()
                            except BrokenPipeError:
                                pass  # The retained file remains authoritative.
                    except BaseException as error:
                        stream_errors.append(f"{type(error).__name__}: {error}")
                    finally:
                        source.close()

                threads = [threading.Thread(target=pump, args=triple) for triple in
                           ((child.stdout, stdout, sys.stdout), (child.stderr, stderr, sys.stderr))]
                for thread in threads:
                    thread.start()
                try:
                    code = child.wait()
                except BaseException:
                    os.killpg(child.pid, signal.SIGTERM)
                    code = child.wait()
                    raise
                finally:
                    for thread in threads:
                        thread.join()
                require(not stream_errors, f"Incomplete stream capture: {stream_errors}")
        except BaseException as error:
            problem = f"{type(error).__name__}: {error}"
        # Retention happens even on failure, before releasing the exclusive lock.
        new_runs = sorted(p for p in runs.iterdir() if p.name not in previous) if runs.exists() else []
        retained = []
        try:
            for run in new_runs:
                require(not run.is_symlink() and run.is_dir(), "Unexpected toolchain run entry")
                output = destination / "toolchain" / run.name
                for source in sorted(run.rglob("*")):
                    require(not source.is_symlink(), f"Symlink in run: {source}")
                    if source.is_file():
                        retained.append(retain_text(source, output / source.relative_to(run)))
            for relative in args.retain_text:
                source = root / relative
                require(not Path(relative).is_absolute() and ".." not in Path(relative).parts and
                        source.resolve().is_relative_to(root) and source.exists(), f"Unsafe/missing retention path: {relative}")
                sources = sorted(source.rglob("*")) if source.is_dir() else [source]
                for item in sources:
                    require(not item.is_symlink(), f"Symlink in extra retention: {item}")
                    if item.is_file():
                        retained.append(retain_text(item, destination / "extra" / item.relative_to(root)))
        except (ValueError, OSError, UnicodeError) as error:
            problem = f"{problem or ''} Retention failed: {error}".strip()
        try:
            require(problem is None, problem)
            if args.toolchain:
                require(len(new_runs) == 1, "Expected one new actual toolchain run; inspect retained failure")
                result_dir = destination / "toolchain" / new_runs[0].name
                require((result_dir / "run.log").is_file(), "Actual run.log missing from retained run")
                result = json.loads((result_dir / "result.json").read_bytes())
                require(result["runDirectory"] == str(new_runs[0].relative_to(root)), "Result belongs to another run")
                canonical_command = command if command[2:3] == ["--"] else command[:2] + ["--"] + command[2:]
                require(shlex.split(result["command"]) == canonical_command, "Actual result command differs from invocation")
                text = (result_dir / "result.txt").read_text()
                require("RESULT: PASSED" in text and "Warnings: 0" in text, "RESULT text disagrees")
                receipt["result_counts"] = check_result(result, code, args.expect_target, args.expect_project)
                for project in result["projects"]:
                    log = (root / project["log"]).resolve()
                    require(log.is_relative_to(new_runs[0].resolve()) and
                            (result_dir / log.relative_to(new_runs[0].resolve())).is_file(), "Actual project step log was not retained")
                if result["projects"]:
                    totals = receipt["result_counts"]
                    require(f"tests: {totals['executed']} executed, 0 failed, {totals['skipped']} skipped" in text,
                            "RESULT text/JSON execution totals disagree")
            else:
                require(code == 0, f"Command exited {code}")
        except (ValueError, KeyError, OSError, TypeError) as error:
            problem = str(error)
        receipt.update(exit_code=code, status="failed" if problem else "passed", error=problem,
                       finished=datetime.datetime.now(datetime.timezone.utc).isoformat(), retained=retained,
                       streams=[digest(destination / name) for name in ("stdout.log", "stderr.log") if (destination / name).exists()])
        (destination / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
        print(f"\nRetained command: {destination}; status={receipt['status']}")
        return 1 if problem else 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--label", required=True)
    parser.add_argument("--cwd", type=Path, default=ROOT)
    parser.add_argument("--toolchain", action="store_true")
    parser.add_argument("--expect-target", action="append", default=[])
    parser.add_argument("--expect-project", action="append", default=[], help="Exact result target:short-project-name; repeat")
    parser.add_argument("--expect-projects-file", type=Path, help="JSON array of exact target:short-project-name strings")
    parser.add_argument("--retain-text", action="append", default=[], help="Root-relative generated text file/directory to preserve after command")
    parser.add_argument("command", nargs=argparse.REMAINDER)
    def interrupted(signum, frame):
        raise KeyboardInterrupt(f"Logger received signal {signum}")
    signal.signal(signal.SIGTERM, interrupted)
    try:
        sys.exit(capture(parser.parse_args()))
    except (ValueError, OSError) as error:
        raise SystemExit(f"Capture blocked: {error}")
