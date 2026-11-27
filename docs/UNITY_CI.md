# Unity CI

工作流：`.github/workflows/unity-ci.yml`。项目结构规则由 `tools/check_unity_project.py` 检查，规则自身的回归测试位于 `tests/ci/`。

## 两层检查

| 作业 | 运行环境 | 内容 | 失败时 |
|---|---|---|---|
| `static-checks` | GitHub-hosted Ubuntu | 安装 numpy；检查 Unity 项目结构、资源与版本钉住；运行全部 unittest CI 测试 | 阻止后续 EditMode 作业 |
| `editmode-tests` | owner 的 Windows self-hosted runner，Unity 6000.3.29f1 | 使用该机器已激活的 Unity Personal 许可证，启动 Unity batchmode，编译全部脚本并运行 EditMode 测试，上传 XML 与 Editor log | Job 失败；编译错误、测试失败或没有结果 XML 都会失败 |

## 哪些事件会运行 EditMode

- 推送到 `main`：运行。
- 手动 `workflow_dispatch`：运行。
- 同仓库分支发起的 Pull Request：运行，因为这些分支来自有仓库写权限的可信协作者。
- 外部 fork Pull Request：只运行 GitHub-hosted 静态检查，EditMode 会跳过。不要让公开仓库的 fork 代码在 owner 的自托管电脑上执行。

EditMode 依赖 `static-checks`。静态检查失败时，Unity 作业不会启动；因此必须先确保静态检查真正通过，而不能把 skipped 当作通过。

## 版本与 Runner

- Editor 版本固定为 `6000.3.29f1`，并与 `ProjectSettings/ProjectVersion.txt` 比对。
- Windows Runner 需要标签：`self-hosted`、`windows`、`x64`、`post-school-unity`。
- Runner 必须在线，且机器上必须安装注册表可定位的同一 Unity Editor 版本。
- Unity 许可证由 Windows 机器上已登录并激活的 Unity Personal 提供；此工作流不读取仓库中的许可证密钥。
- 当前 Windows 主机通过 Task Scheduler 登录触发任务启动 Runner；Runner 在该用户完成交互式登录后上线。这是登录启动任务，不是 Windows 服务。

## 本地静态检查

```bash
python3 tools/check_unity_project.py --check
python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
```

Windows PowerShell 可使用：

```powershell
py -3 tools/check_unity_project.py --check
py -3 -m unittest discover -s tests/ci -p 'test_*.py' -v
```

## 本地 EditMode 检查

在关闭同一项目的其他 Unity Editor 实例后，从仓库根目录运行（Unity 路径按本机安装位置调整）：

```powershell
& 'E:\Programs\Unity\Editors\6000.3.29f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath (Get-Location).Path `
  -runTests -testPlatform EditMode `
  -testResults "$env:TEMP\PostSchool-EditMode-results.xml" `
  -logFile "$env:TEMP\PostSchool-EditMode.log"
```

只有 Unity 进程退出码为 0、结果 XML 存在且 `/test-run` 显示 `Passed`，才可报告 EditMode 通过。Unity 没有生成 XML（例如 C# 编译失败）也必须视为失败。

## 当前不覆盖

- 目前覆盖静态项目检查和 EditMode；PlayMode、Windows Build、实际画面捕获尚未加入这个工作流。
- 静态检查不能证明 C# 可以编译；Unity EditMode 作业负责真实的脚本编译门槛。
- PR 上 EditMode 为 skipped 时，先检查 PR 来源是否为外部 fork；外部 fork 的跳过是安全策略，不是测试通过。
