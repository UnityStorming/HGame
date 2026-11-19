"""验证 MCP 同步的外部行为，使用临时工程避免修改真实配置。"""

import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

try:
    import tomllib
except ImportError:
    import tomli as tomllib


SCRIPT = Path(__file__).resolve().parents[1] / "sync_mcp.py"
HOOKS = '''# 用户保留的 hooks
[[hooks.Stop]]
[[hooks.Stop.hooks]]
type = "command"
command = 'python ".codex/hooks/codex_popup.py"'
timeout = 600
'''


class SyncMcpTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.codex = self.root / ".codex"
        self.codex.mkdir()
        self.config = self.codex / "config.toml"
        self.config.write_text(
            '[mcp_servers.rider]\nenabled = true\nstartup_timeout_sec = 30\n'
            'tool_timeout_sec = 300\ncommand = "old"\n\n' + HOOKS,
            encoding="utf-8",
        )
        self.servers = {
            "unity-cli": {"command": "unity", "args": ["mcp", "--project-path", "GameUnity"],
                          "env": {"UNITY_NO_BANNER": "1"}},
            "rider": {"command": "python", "args": ["-m", "mcp_proxy", "http://localhost:64342/sse"]},
        }
        self.write_source()

    def write_source(self):
        (self.codex / "mcp.json").write_text(
            json.dumps({"mcpServers": self.servers}, ensure_ascii=False), encoding="utf-8"
        )

    def run_sync(self, *args):
        self.assertTrue(SCRIPT.is_file(), "尚未实现 MCP 同步脚本")
        return subprocess.run(
            [sys.executable, str(SCRIPT), "--root", str(self.root), *args],
            capture_output=True, text=True, encoding="utf-8",
        )

    def test_sync_preserves_hooks_and_codex_options(self):
        result = self.run_sync()
        self.assertEqual(result.returncode, 0, result.stderr)
        text = self.config.read_text(encoding="utf-8")
        data = tomllib.loads(text)
        self.assertIn(HOOKS, text)
        self.assertEqual(data["mcp_servers"]["unity-cli"], self.servers["unity-cli"])
        rider = data["mcp_servers"]["rider"]
        self.assertEqual(rider["command"], "python")
        self.assertEqual(rider["args"], self.servers["rider"]["args"])
        self.assertEqual(rider["startup_timeout_sec"], 30)
        self.assertEqual(rider["tool_timeout_sec"], 300)
        self.assertTrue(rider["enabled"])

    def test_check_detects_drift_without_writing(self):
        before = self.config.read_bytes()
        result = self.run_sync("--check")
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.config.read_bytes(), before)

    def test_repeat_sync_is_idempotent(self):
        self.assertEqual(self.run_sync().returncode, 0)
        before = self.config.read_bytes()
        self.assertEqual(self.run_sync().returncode, 0)
        self.assertEqual(self.config.read_bytes(), before)
        self.assertEqual(self.run_sync("--check").returncode, 0)

    def test_removed_server_is_removed_from_toml(self):
        del self.servers["rider"]
        self.write_source()
        result = self.run_sync()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("rider", tomllib.loads(self.config.read_text(encoding="utf-8"))["mcp_servers"])

    def test_invalid_source_does_not_write(self):
        self.servers["rider"]["args"] = "not-an-array"
        self.write_source()
        before = self.config.read_bytes()
        result = self.run_sync()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.config.read_bytes(), before)

    def test_json_strings_and_http_headers_round_trip(self):
        self.servers = {"docs": {"url": "https://example.com/mcp", "headers": {"X-Text": '中文\\路径"\n第二行'}}}
        self.write_source()
        result = self.run_sync()
        self.assertEqual(result.returncode, 0, result.stderr)
        server = tomllib.loads(self.config.read_text(encoding="utf-8"))["mcp_servers"]["docs"]
        self.assertEqual(server["url"], self.servers["docs"]["url"])
        self.assertEqual(server["http_headers"], self.servers["docs"]["headers"])

    def test_nested_mcp_tables_do_not_leave_stale_env(self):
        self.config.write_text('[mcp_servers.rider]\ncommand = "old"\n[mcp_servers.rider.env]\nOLD = "1"\n\n' + HOOKS, encoding="utf-8")
        result = self.run_sync()
        self.assertEqual(result.returncode, 0, result.stderr)
        data = tomllib.loads(self.config.read_text(encoding="utf-8"))
        self.assertNotIn("env", data["mcp_servers"]["rider"])
        self.assertIn(HOOKS, self.config.read_text(encoding="utf-8"))

    def test_root_settings_keep_their_toml_scope(self):
        self.config.write_text('model = "test-model"\napproval_policy = "on-request"\n\n' + HOOKS, encoding="utf-8")
        result = self.run_sync()
        self.assertEqual(result.returncode, 0, result.stderr)
        data = tomllib.loads(self.config.read_text(encoding="utf-8"))
        self.assertEqual(data["model"], "test-model")
        self.assertEqual(data["approval_policy"], "on-request")
        self.assertEqual(set(data["mcp_servers"]), set(self.servers))


if __name__ == "__main__":
    unittest.main()
