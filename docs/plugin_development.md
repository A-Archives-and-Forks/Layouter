# Layouter Plugin Development Documentation

This document applies to the Layouter plugin development mode. Plugins are local development capabilities, intended for trusted developers by default; plugin code is dynamically compiled and executed within the Layouter process.

## Plugin Package Format

Plugin packages have the fixed extension `.plug`, essentially a zip archive. Files can be placed directly within the package, or a directory with the same name can be used to wrap the files. The following filenames are consistently recognized at runtime:

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

Required Files:

- `plugin.json`: Plugin manifest.
- `CodeFilePath` points to the C# source code file.

Optional files:

- `icon.json`: Mapping of feature names to icon paths or shell-specific paths.
- `style.json`: Default style for the plugin window.
- `icons/`: Image resource directory.
- `libs/`: DLLs required for plugin compilation and operation.

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

Field Descriptions:

- `ProtocolVersion`: Currently fixed at `1.0`.
- `Id`: Stable and unique identifier for the plugin. User configuration and enabled status are stored using this value.
- `Key`: Short identifier for presentation, file, or business semantics.
- `PluginClassName`: The plugin class name in the source code.
- `Style`: Window template, `1` for card/section, `2` for detail window, `3` for floating window.
- `IsEnabled`: The plugin's default enabled state. User modifications will be saved to the local user configuration and will not rewrite the plugin package.
- `CodeFilePath`: The source code path relative to the directory containing `plugin.json`.
- `DevelopmentMode`: Plugin development mode flag.
- `AllowUnsafeApis`: Defaults to `false`. Setting it to `true` will allow some high-risk APIs, only for development and debugging.

## Plug-in code

Plugin classes must implement `PluginEntry.IPlugin`:

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

`Register()` should only register functions, not perform long-running blocking operations. Periodic refresh is driven by the window template.

## icon.json

`icon.json` is a mapping from function names to resource paths:

```json
{
  "Demo": "icons/demo.png",
  "设备管理器": "::{74246bfc-4c96-11d0-abef-0020af6b0b7a}"
}
```

Relative paths will be resolved according to the directory where `icon.json` is located. Shell special paths starting with `::` will be preserved as is.

## style.json

`style.json` is the plugin's default style. After a user modifies a style in the interface, the Layouter saves the user configuration locally, without modifying `.plug`.

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

## User Configuration

User-level configuration location:

```text
%LocalAppData%\Layouter\Plugins\{pluginId}.json
```

This saves the user's enabled status and user style overwrites. Deleting the plugin will also delete the corresponding user configuration.

## Security Verification

The Layouter performs static validation before loading plugin source code:

- Plugin classes must implement `IPlugin`.
- High-risk modes such as `Process.Start`, `File.Delete`, `Directory.Delete`, `DllImport`, `Assembly.Load`, and `Environment.Exit` are blocked by default.
- Warnings are issued for sensitive namespaces such as `System.IO`, `System.Net`, and `System.Reflection`.

Note: This is development mode security verification, not a strong sandbox. Plugins ultimately execute within the Layouter process. Do not install or run plugins from untrusted sources.

## Release Recommendations

- Use stable and unchanging `Id`s.
- Keep `PluginClassName` consistent with the source code class name.
- Do not rely on `.plug` filenames in runtime logic.
- Place third-party DLLs in `libs/`.
- User-configurable items should be saved via plugin parameters or user configuration; do not modify the plugin package.