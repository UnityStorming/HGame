import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[1] / "setup-agent-links.ps1"
SHELL = shutil.which("pwsh") or shutil.which("powershell")
LINKS = {".claude/skills": "../.codex/skills", ".cursor/skills": "../.codex/skills",
         ".agents/skills": "../.codex/skills", ".mcp.json": ".codex/mcp.json",
         ".cursor/mcp.json": "../.codex/mcp.json"}


@unittest.skipUnless(SHELL, "PowerShell required")
class AgentLinksTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / ".codex/skills/dummy").mkdir(parents=True)
        (self.root / ".codex/skills/dummy/SKILL.md").write_text("dummy", encoding="utf-8")
        (self.root / ".codex/mcp.json").write_text(
            json.dumps({"mcpServers": {"dummy": {"command": "dummy", "args": []}}}), encoding="utf-8")
        (self.root / ".codex/config.toml").write_text('model = "test"\n', encoding="utf-8")

    def run_script(self, check=False):
        self.assertTrue(SCRIPT.is_file(), "setup-agent-links.ps1 missing")
        args = [SHELL, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(SCRIPT),
                "-ProjectRoot", str(self.root)]
        if check:
            args.append("-Check")
        return subprocess.run(args, capture_output=True, text=True)

    def assert_ok(self, result):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_create_and_idempotent(self):
        self.assert_ok(self.run_script())
        for name, target in LINKS.items():
            link = self.root / name
            self.assertTrue(link.is_symlink(), name)
            self.assertEqual(link.readlink().as_posix(), target)
        config = (self.root / ".codex/config.toml").read_bytes()
        self.assert_ok(self.run_script())
        self.assertEqual(config, (self.root / ".codex/config.toml").read_bytes())
        self.assert_ok(self.run_script(check=True))

    def test_check_drift_is_read_only(self):
        self.assert_ok(self.run_script())
        config = self.root / ".codex/config.toml"
        config.write_text('model = "test"\n', encoding="utf-8")
        before = config.read_bytes()
        self.assertNotEqual(self.run_script(check=True).returncode, 0)
        self.assertEqual(before, config.read_bytes())

    def test_wrong_target_rejected(self):
        (self.root / ".cursor").mkdir()
        (self.root / ".cursor/skills").symlink_to("../wrong", target_is_directory=True)
        before = (self.root / ".codex/config.toml").read_bytes()
        self.assertNotEqual(self.run_script().returncode, 0)
        self.assertFalse((self.root / ".claude/skills").exists())
        self.assertEqual(before, (self.root / ".codex/config.toml").read_bytes())

    def test_nonempty_directory_rejects_before_writes(self):
        occupied = self.root / ".agents/skills"
        occupied.mkdir(parents=True)
        (occupied / "keep").write_bytes(b"keep")
        before = (self.root / ".codex/config.toml").read_bytes()
        self.assertNotEqual(self.run_script().returncode, 0)
        self.assertFalse((self.root / ".claude").exists())
        self.assertEqual(b"keep", (occupied / "keep").read_bytes())
        self.assertEqual(before, (self.root / ".codex/config.toml").read_bytes())

    def test_serialized_git_links_restored(self):
        for name, target in LINKS.items():
            path = self.root / name
            path.parent.mkdir(exist_ok=True)
            path.write_text(target, encoding="utf-8")
        self.assertNotEqual(self.run_script(check=True).returncode, 0)
        self.assert_ok(self.run_script())
        self.assertTrue(all((self.root / name).is_symlink() for name in LINKS))

    def test_empty_directory_and_equal_json_allowed(self):
        (self.root / ".claude/skills").mkdir(parents=True)
        (self.root / ".mcp.json").write_bytes((self.root / ".codex/mcp.json").read_bytes())
        self.assert_ok(self.run_script())

    def test_different_json_rejects_before_writes(self):
        (self.root / ".mcp.json").write_text('{}', encoding="utf-8")
        self.assertNotEqual(self.run_script().returncode, 0)
        self.assertFalse((self.root / ".claude").exists())

    def test_missing_source_rejected_without_writes(self):
        (self.root / ".codex/mcp.json").unlink()
        self.assertNotEqual(self.run_script().returncode, 0)
        self.assertFalse((self.root / ".claude").exists())

    def test_parent_link_rejected_without_writes(self):
        (self.root / "elsewhere").mkdir()
        (self.root / ".agents").symlink_to("elsewhere", target_is_directory=True)
        self.assertNotEqual(self.run_script().returncode, 0)
        self.assertFalse((self.root / ".claude").exists())
        self.assertFalse((self.root / "elsewhere/skills").exists())


if __name__ == "__main__":
    unittest.main()
