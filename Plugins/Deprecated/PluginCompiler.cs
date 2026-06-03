using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.CSharp;

namespace Layouter.Plugins
{
    public class PluginCompiler
    {
        public static bool CompilePlugin(string sourceFile, string outputFile, List<string> references = null)
        {
            try
            {
                // 读取源代码
                string sourceCode = File.ReadAllText(sourceFile);

                // 创建编译器
                var provider = new CSharpCodeProvider();
                var parameters = new CompilerParameters();

                // 添加基本引用
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                parameters.ReferencedAssemblies.Add("System.Xaml.dll");
                parameters.ReferencedAssemblies.Add("WindowsBase.dll");
                parameters.ReferencedAssemblies.Add("PresentationCore.dll");
                parameters.ReferencedAssemblies.Add("PresentationFramework.dll");

                // 添加Layouter程序集引用
                parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

                // 添加其他引用
                if (references != null)
                {
                    foreach (var reference in references)
                    {
                        parameters.ReferencedAssemblies.Add(reference);
                    }
                }

                // 设置输出文件
                parameters.OutputAssembly = outputFile;
                parameters.GenerateExecutable = false;
                parameters.GenerateInMemory = false;

                // 编译
                CompilerResults results = provider.CompileAssemblyFromSource(parameters, sourceCode);

                // 检查编译错误
                if (results.Errors.HasErrors)
                {
                    foreach (CompilerError error in results.Errors)
                    {
                        Console.WriteLine($"编译错误: {error.ErrorText} (行 {error.Line})");
                    }

                    return false;
                }

                Console.WriteLine($"插件编译成功: {outputFile}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"编译插件时出错: {ex.Message}");
                return false;
            }
        }
    }
}
