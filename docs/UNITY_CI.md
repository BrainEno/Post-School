# Unity CI

工作流：`.github/workflows/unity-ci.yml`。对应规则：`tools/check_unity_project.py`，其判定测试在 `tests/ci/`。

## 两层检查

| 作业 | 是否需要 Unity | 内容 | 失败时 |
|---|---|---|---|
| `static-checks` | 否 | 项目树检查（禁止提交的生成目录与 IDE 文件、`.meta` 配对与 GUID 唯一、JSON / asmdef 可解析、Assets 中无 CRLF、版本钉住一致、对话数据引用完整）；检查脚本自身的判定测试 | 阻止合并 |
| `editmode-tests` | 是（6000.3.29f1） | `game-ci/unity-test-runner` 运行 EditMode 测试，结果作为 artifact 上传 | 阻止合并（前提：已配置许可证） |

`editmode-tests` 依赖 `static-checks`。未配置 `UNITY_LICENSE` 时，该作业不会假装通过：会发出 warning，并在步骤摘要中写明 **NOT RUN**。

## 版本钉住

- Editor 版本只在 `ProjectSettings/ProjectVersion.txt` 与工作流的 `unityVersion` 中各写一次，两者必须一致，检查脚本会比对。
- `unityVersion` 不使用 `auto`，避免运行时读取版本文件而掩盖漂移。
- 升级 Editor 需要 owner 明确批准（见 `AGENTS.md`）。

## 配置 Unity 许可证（一次性，需要 owner 操作）

1. 在仓库的 Settings → Secrets and variables → Actions 中新增 `UNITY_LICENSE`。
2. 值为 Unity 许可证激活文件（`.ulf`）的完整内容，由 owner 在本机按 game-ci 文档生成。
3. 不要把许可证内容写入仓库、Issue、PR 或日志。

配置前，`editmode-tests` 只会输出 warning；配置后，它会真正运行测试。

## 本地运行

```bash
python3 tools/check_unity_project.py --check
python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
```

Editor 测试（在项目根目录，替换为本机 Unity 6.3 可执行文件）：

```powershell
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults "Temp\editmode-results.xml" -logFile "Temp\editmode.log"
```

## 已知限制

- Unity Editor 镜像体积较大，GitHub 托管 runner 的磁盘空间可能不足；若出现，需要改为自托管 runner 或精简镜像。
- 目前只跑 EditMode。PlayMode、构建与 Windows 目标尚未加入。
- `static-checks` 不能证明 C# 能编译；编译错误只有 `editmode-tests` 能发现。
