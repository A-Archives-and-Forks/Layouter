# Layouter 插件开发文档

本文档适用于 Layouter 插件开发模式。插件是本地开发能力，默认面向可信开发者；插件代码会被动态编译并在 Layouter 进程内执行。

## 插件包格式

插件包扩展名固定为 `.plug`，本质是 zip 压缩包。包内可以直接放文件，也可以使用一个同名目录包裹文件。运行时固定识别以下文件名：

```text
MyPlugin.plug
├── plugin.json
├── MyPlugin.cs
├── icon.json
├── style.json
├── icons/
│   └── action.png
└── libs/
    └── ThirdParty.dll
```

必需文件：

- `plugin.json`: 插件清单。
- `CodeFilePath` 指向的 C# 源码文件。

可选文件：

- `icon.json`: 功能名到图标路径或 Shell 特殊路径的映射。
- `style.json`: 插件窗口默认样式。
- `icons/`: 图片资源目录。
- `libs/`: 插件编译和运行需要的 DLL。

## plugin.json

```json
{
  "ProtocolVersion": "1.0",
  "Id": "A stable unique id",
  "Key": "my-plugin",
  "Name": "插件名称",
  "PluginClassName": "MyPlugin",
  "Version": "1.0.0",
  "Description": "插件说明",
  "Author": "Author",
  "Style": 2,
  "IsEnabled": true,
  "CodeFilePath": "./MyPlugin.cs",
  "DevelopmentMode": true,
  "AllowUnsafeApis": false
}
```

字段说明：

- `ProtocolVersion`: 当前固定为 `1.0`。
- `Id`: 插件稳定唯一标识。用户配置和启用状态都按这个值保存。
- `Key`: 面向展示、文件或业务语义的短标识。
- `PluginClassName`: 源码中的插件类名。
- `Style`: 窗口模板，`1` 为卡片/分区，`2` 为明细窗口，`3` 为悬浮窗口。
- `IsEnabled`: 插件默认启用状态。用户修改后会保存到本地用户配置，不会改写插件包。
- `CodeFilePath`: 相对 `plugin.json` 所在目录的源码路径。
- `DevelopmentMode`: 插件开发模式标记。
- `AllowUnsafeApis`: 默认 `false`。设为 `true` 会允许部分高风险 API，仅用于开发调试。

## 插件代码

插件类必须实现 `PluginEntry.IPlugin`：

```csharp
using PluginEntry;

public class MyPlugin : IPlugin
{
    public string Name => "插件名称";

    public Dictionary<string, Func<PluginParameter[], object>> FunctionDict { get; } = new();

    public void Register()
    {
        FunctionDict["Demo"] = Demo;
    }

    public void Unregister()
    {
        FunctionDict.Clear();
    }

    public object Run(string functionKey, params PluginParameter[] parameters)
    {
        return FunctionDict.TryGetValue(functionKey, out var action)
            ? action(parameters)
            : null;
    }

    public Dictionary<string, List<PluginParameter>> GetParameterDescriptions()
    {
        return new Dictionary<string, List<PluginParameter>>();
    }

    private object Demo(PluginParameter[] parameters)
    {
        return "OK";
    }
}
```

`Register()` 只应注册函数，不要做长时间阻塞操作。周期刷新由窗口模板驱动。

## icon.json

`icon.json` 是功能名到资源路径的映射：

```json
{
  "Demo": "icons/demo.png",
  "设备管理器": "::{74246bfc-4c96-11d0-abef-0020af6b0b7a}"
}
```

相对路径会按 `icon.json` 所在目录解析。以 `::` 开头的 Shell 特殊路径会原样保留。

## style.json

`style.json` 是插件的默认样式。用户在界面中修改样式后，Layouter 会将用户配置保存到本地，不会修改 `.plug`。

```json
{
  "WindowPosition": {
    "Left": 100,
    "Top": 100,
    "Width": 300,
    "Height": 180
  },
  "Opacity": 0.7,
  "BackgroundColor": "#FF000000",
  "ForegroundColor": "#FFFFFFFF",
  "PercentageMode": true,
  "CycleExecution": true,
  "Inteval": 1
}
```

## 用户配置

用户级配置位置：

```text
%LocalAppData%\Layouter\Plugins\{pluginId}.json
```

这里保存用户启用状态和用户样式覆盖。删除插件时，对应用户配置也会删除。

## 安全验证

Layouter 会在加载插件源码前做静态验证：

- 插件类必须实现 `IPlugin`。
- 默认阻止 `Process.Start`、`File.Delete`、`Directory.Delete`、`DllImport`、`Assembly.Load`、`Environment.Exit` 等高风险模式。
- 对 `System.IO`、`System.Net`、`System.Reflection` 等敏感命名空间给出警告。

注意：这是开发模式安全验证，不是强沙箱。插件最终仍在 Layouter 进程内执行。不要安装或运行不可信来源的插件。

## 发布建议

- 使用稳定不变的 `Id`。
- 保持 `PluginClassName` 和源码类名一致。
- 不要依赖 `.plug` 文件名参与运行时逻辑。
- 第三方 DLL 放入 `libs/`。
- 用户可配置项优先通过插件参数或用户配置保存，不要修改插件包。
