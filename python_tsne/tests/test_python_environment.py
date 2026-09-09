from __future__ import annotations

import base64
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


HELPER_PATH = Path(__file__).resolve().parents[1] / "scripts" / "python_environment.ps1"
RESULT_PREFIX = "TAS_ENV_RESULT="


@unittest.skipUnless(
    sys.platform == "win32" and sys.version_info[:2] == (3, 12),
    "Windows environment recovery requires running the tests with Python 3.12.",
)
class PythonEnvironmentTests(unittest.TestCase):
    """Exercise recovery against temporary environments without installing dependencies."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.powershell = shutil.which("powershell.exe")
        if cls.powershell is None:
            raise unittest.SkipTest("Windows PowerShell is unavailable.")
        cls.base_python = Path(sys._base_executable).resolve()
        cls.python311: Path | None = None
        launcher = shutil.which("py.exe")
        if launcher:
            discovered = subprocess.run(
                [launcher, "-3.11", "-c", "import sys; print(sys.executable)"],
                capture_output=True,
                text=True,
                timeout=20,
                check=False,
            )
            if discovered.returncode == 0:
                cls.python311 = Path(discovered.stdout.strip())

    def setUp(self) -> None:
        temporary_directory = tempfile.TemporaryDirectory(prefix="tas env recovery ")
        self.addCleanup(temporary_directory.cleanup)
        self.project = Path(temporary_directory.name) / "project with spaces"
        self.project.mkdir()
        self.venv = self.project / ".venv"
        self.venv_python = self.venv / "Scripts" / "python.exe"

    def create_venv(self, python: Path | None = None) -> None:
        subprocess.run(
            [str(python or self.base_python), "-m", "venv", "--without-pip", str(self.venv)],
            check=True,
            capture_output=True,
            text=True,
            timeout=60,
        )

    def write_sentinel(self) -> Path:
        self.venv.mkdir(exist_ok=True)
        sentinel = self.venv / "preserve-existing-files.txt"
        sentinel.write_text("This existing environment must remain recoverable.\n", encoding="utf-8")
        return sentinel

    def run_helper(
        self, python: Path | None = None, *, recreate: bool = False
    ) -> subprocess.CompletedProcess[str]:
        environment = os.environ.copy()
        environment.pop("PYTHONHOME", None)
        environment.pop("PYTHONPATH", None)
        environment["TAS_TEST_ENV_HELPER"] = str(HELPER_PATH)
        environment["TAS_TEST_ENV_PROJECT"] = str(self.project)
        environment["TAS_TEST_ENV_PYTHON"] = str(python or self.base_python)
        environment["TAS_TEST_ENV_RECREATE"] = "1" if recreate else "0"
        # Environment variables carry paths so spaces and quotes are never interpreted as code.
        script = """
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
. $env:TAS_TEST_ENV_HELPER
$parameters = @{
    ProjectRoot = $env:TAS_TEST_ENV_PROJECT
    PythonExecutable = $env:TAS_TEST_ENV_PYTHON
    RecreateVenv = ($env:TAS_TEST_ENV_RECREATE -eq '1')
}
$result = Initialize-Python312Environment @parameters
Write-Output ('TAS_ENV_RESULT=' + $result)
"""
        encoded_script = base64.b64encode(script.encode("utf-16-le")).decode("ascii")
        return subprocess.run(
            [
                self.powershell,
                "-NoLogo",
                "-NoProfile",
                "-NonInteractive",
                "-ExecutionPolicy",
                "Bypass",
                "-EncodedCommand",
                encoded_script,
            ],
            cwd=self.project,
            env=environment,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=120,
            check=False,
        )

    def assert_ready(self, result: subprocess.CompletedProcess[str]) -> None:
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        reported_paths = [
            line.removeprefix(RESULT_PREFIX)
            for line in result.stdout.splitlines()
            if line.startswith(RESULT_PREFIX)
        ]
        self.assertEqual([str(self.venv_python)], reported_paths, result.stdout)
        runtime = subprocess.run(
            [
                str(self.venv_python),
                "-I",
                "-c",
                "import json, sys; print(json.dumps([list(sys.version_info[:2]), sys.prefix]))",
            ],
            check=True,
            capture_output=True,
            text=True,
            timeout=20,
        )
        version, prefix = json.loads(runtime.stdout)
        self.assertEqual([3, 12], version)
        self.assertEqual(self.venv.resolve(), Path(prefix).resolve())

    def assert_preserved_backup(self, sentinel_text: str) -> Path:
        backups = list(self.project.glob(".venv.backup-*"))
        self.assertEqual(1, len(backups))
        backup = backups[0]
        self.assertEqual(
            sentinel_text,
            (backup / "preserve-existing-files.txt").read_text(encoding="utf-8"),
        )
        self.assertFalse((self.venv / "preserve-existing-files.txt").exists())
        return backup

    def test_healthy_python312_environment_is_reused(self) -> None:
        self.create_venv()
        sentinel = self.write_sentinel()
        original_config = (self.venv / "pyvenv.cfg").read_bytes()
        original_python_mtime = self.venv_python.stat().st_mtime_ns

        self.assert_ready(self.run_helper())

        self.assertTrue(sentinel.is_file())
        self.assertEqual(original_config, (self.venv / "pyvenv.cfg").read_bytes())
        self.assertEqual(original_python_mtime, self.venv_python.stat().st_mtime_ns)
        self.assertEqual([], list(self.project.glob(".venv.backup-*")))

    def test_incomplete_environment_is_backed_up_and_recreated(self) -> None:
        sentinel_text = self.write_sentinel().read_text(encoding="utf-8")

        self.assert_ready(self.run_helper())

        self.assert_preserved_backup(sentinel_text)

    def test_copied_environment_with_missing_base_python_is_recovered(self) -> None:
        self.create_venv()
        sentinel_text = self.write_sentinel().read_text(encoding="utf-8")
        config_path = self.venv / "pyvenv.cfg"
        missing_home = self.project / "missing original Python installation"
        config_path.write_text(
            "\n".join(
                f"home = {missing_home}" if line.startswith("home = ") else line
                for line in config_path.read_text(encoding="utf-8").splitlines()
            )
            + "\n",
            encoding="utf-8",
        )
        broken_config = config_path.read_bytes()
        broken_runtime = subprocess.run(
            [str(self.venv_python), "-I", "-c", "import sys; print(sys.version)"],
            capture_output=True,
            text=True,
            timeout=20,
            check=False,
        )
        self.assertNotEqual(0, broken_runtime.returncode, "The copied-environment fixture must fail.")

        self.assert_ready(self.run_helper())

        backup = self.assert_preserved_backup(sentinel_text)
        self.assertEqual(broken_config, (backup / "pyvenv.cfg").read_bytes())

    def test_missing_explicit_python_leaves_existing_environment_untouched(self) -> None:
        self.create_venv()
        sentinel = self.write_sentinel()
        sentinel_text = sentinel.read_text(encoding="utf-8")
        original_config = (self.venv / "pyvenv.cfg").read_bytes()

        result = self.run_helper(self.project / "missing Python" / "python.exe", recreate=True)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(sentinel_text, sentinel.read_text(encoding="utf-8"))
        self.assertEqual(original_config, (self.venv / "pyvenv.cfg").read_bytes())
        self.assertTrue(self.venv_python.is_file())
        self.assertEqual([], list(self.project.glob(".venv.backup-*")))

    def test_requested_recreation_preserves_healthy_environment(self) -> None:
        self.create_venv()
        sentinel_text = self.write_sentinel().read_text(encoding="utf-8")
        original_config = (self.venv / "pyvenv.cfg").read_bytes()

        self.assert_ready(self.run_helper(recreate=True))

        backup = self.assert_preserved_backup(sentinel_text)
        self.assertEqual(original_config, (backup / "pyvenv.cfg").read_bytes())

    def test_recreation_can_resolve_base_python_from_existing_environment(self) -> None:
        self.create_venv()
        sentinel_text = self.write_sentinel().read_text(encoding="utf-8")

        self.assert_ready(self.run_helper(self.venv_python, recreate=True))

        self.assert_preserved_backup(sentinel_text)

    def test_wrong_version_environment_is_backed_up_and_recreated(self) -> None:
        if self.python311 is None:
            self.skipTest("Optional Python 3.11 fixture is unavailable.")
        self.create_venv(self.python311)
        sentinel_text = self.write_sentinel().read_text(encoding="utf-8")
        original_config = (self.venv / "pyvenv.cfg").read_bytes()

        self.assert_ready(self.run_helper())

        backup = self.assert_preserved_backup(sentinel_text)
        self.assertEqual(original_config, (backup / "pyvenv.cfg").read_bytes())

    def test_wrong_explicit_python_leaves_existing_environment_untouched(self) -> None:
        if self.python311 is None:
            self.skipTest("Optional Python 3.11 fixture is unavailable.")
        sentinel = self.write_sentinel()
        sentinel_text = sentinel.read_text(encoding="utf-8")

        result = self.run_helper(self.python311)

        self.assertNotEqual(0, result.returncode)
        self.assertEqual(sentinel_text, sentinel.read_text(encoding="utf-8"))
        self.assertEqual([sentinel], list(self.venv.iterdir()))
        self.assertEqual([], list(self.project.glob(".venv.backup-*")))


if __name__ == "__main__":
    unittest.main()
