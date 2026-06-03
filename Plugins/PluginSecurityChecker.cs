using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PluginEntry;

namespace Layouter.Plugins
{
    public class PluginSecurityValidationResult
    {
        public bool IsAllowed { get; set; } = true;

        public List<string> Errors { get; } = new List<string>();

        public List<string> Warnings { get; } = new List<string>();

        public override string ToString()
        {
            var messages = Errors.Concat(Warnings).ToList();
            return messages.Count == 0 ? "No issues" : string.Join("; ", messages);
        }
    }

    public class PluginSecurityChecker
    {
        /// <summary>
        /// 危险API列表
        /// </summary>
        private readonly List<string> _forbiddenPatterns = new List<string>
        {
            @"System\.Diagnostics\.Process\.Start",
            @"System\.IO\.File\.Delete",
            @"System\.Net\.WebClient",
            @"System\.Reflection\.Assembly\.Load",
            @"Microsoft\.Win32\.Registry",
            @"System\.Runtime\.InteropServices",
            @"System\.Security\.AccessControl\.FileSystemAccessRule",
            @"Environment\.Exit",
            @"System\.Diagnostics\.EventLog",
            @"new\s+System\.Net\.Sockets\.TcpClient",
            @"System\.Windows\.Forms\.Application\.Exit",
            @"DllImport\s*\(",
            @"Assembly\.Load",
            @"File\.Delete\s*\(",
            @"Directory\.Delete\s*\(",
            @"Process\.Start\s*\(",
            @"Environment\.Exit\s*\("
        };

        private readonly List<string> _warningPatterns = new List<string>
        {
            @"using\s+System\.IO",
            @"using\s+System\.Net",
            @"using\s+System\.Reflection",
            @"using\s+System\.Diagnostics",
            @"Microsoft\.Win32",
            @"unsafe\s"
        };

        /// <summary>
        /// 代码静态分析
        /// </summary>
        public bool CheckCode(string sourceCode)
        {
            return ValidateCode(sourceCode).IsAllowed;
        }

        public PluginSecurityValidationResult ValidateCode(string sourceCode, PluginDescriptor descriptor = null)
        {
            var result = new PluginSecurityValidationResult();

            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                result.Errors.Add("Plugin source code is empty.");
            }

            foreach (var pattern in _forbiddenPatterns)
            {
                if (Regex.IsMatch(sourceCode, pattern))
                {
                    result.Errors.Add($"Forbidden API pattern detected: {pattern}");
                }
            }

            // 检查其他危险模式
            if (Regex.IsMatch(sourceCode, @"using\s+System\.Diagnostics"))
            {
                // 进一步检查有没有使用危险类
                if (Regex.IsMatch(sourceCode, @"Process\.|ProcessStartInfo"))
                {
                    result.Errors.Add("Process manipulation detected.");
                }
            }

            foreach (var pattern in _warningPatterns)
            {
                if (Regex.IsMatch(sourceCode, pattern))
                {
                    result.Warnings.Add($"Sensitive namespace or keyword detected: {pattern}");
                }
            }

            if (!Regex.IsMatch(sourceCode, @":\s*IPlugin\b") && !Regex.IsMatch(sourceCode, @":\s*PluginEntry\.IPlugin\b"))
            {
                result.Errors.Add("Plugin class must implement IPlugin.");
            }

            if (descriptor?.AllowUnsafeApis == true)
            {
                result.Warnings.Add("AllowUnsafeApis is enabled; forbidden API errors were downgraded for development use.");
                result.Warnings.AddRange(result.Errors);
                result.Errors.Clear();
            }

            result.IsAllowed = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 动态方法安全检查
        /// </summary>
        public bool CheckFunction(dynamic plugin, string functionName)
        {
            try
            {
                // 获取函数实例，假设为动态对象
                var dict = plugin.FunctionDict as Dictionary<string, Func<PluginParameter[], object>>;
                if (dict?.ContainsKey(functionName) != true)
                {
                    return false;
                }
                //Todo: 进一步的排查逻辑
                return true;
            }
            catch
            {
                return false;
            }
        }
    }


    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
    }
}
