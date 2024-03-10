using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Microsoft.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.Text;

namespace ParasiteInvoke
{
    public class Assembly
    {
        private static readonly object consoleLock = new object();

        public static bool IsDotnetAssembly(string filePath)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(filePath);
                return assemblyName != null;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        public static void EnumeratePInvokeMethods(string filePath)
        {
            ProcessPInvokeMethods(filePath, (method, piInfo) =>
            {
                Assembly.ParsePInvokeMethod(method, piInfo);
            });
        }

        public static void FindPInvokeMethod(string filePath, string methodName)
        {
            ProcessPInvokeMethods(filePath, (method, piInfo) =>
            {
                if (piInfo == null)
                    return;

                if (piInfo.EntryPoint == methodName)
                {
                    Console.WriteLine("-------------");
                    Console.WriteLine($"[FILE] {filePath}");
                    Assembly.ParsePInvokeMethod(method, piInfo);
                    Console.WriteLine("-------------");
                    return;
                }
            });
        }

        private static void ParsePInvokeMethod(MethodDefinition method, PInvokeInfo piInfo)
        {
            lock (consoleLock)
            {
                var typeName = method.DeclaringType.FullName;
                Console.WriteLine($"\nMethod: {piInfo.EntryPoint}");
                Console.WriteLine($"\t===PARASITE INVOKE{(typeName.StartsWith("_") ? " Broken" : "")} SIGNATURE===");
                Console.WriteLine($"\tAssembly asm = Assembly.LoadFrom(@\"{method.Module.FileName}\");");
                Console.WriteLine($"\tType t = asm.GetType(\"{typeName}\", true);");
                Console.WriteLine($"\tvar methodInfo = t.GetMethod(\"{piInfo.EntryPoint}\", {DetermineBindingFlags(method)} );");
                Console.WriteLine($"\t{method.ReturnType.FullName} result = ({method.ReturnType.FullName}) methodInfo.Invoke(null, new object[] {{ {ParseArgs(method)} }});"); // first argument is null because PInvoke winapi methods are always static methods
                Console.WriteLine($"\t===END SIGNATURE===");
            }
        }

        private static string ParseArgs(MethodDefinition method)
        {
            if (!method.HasParameters)
            {
                return "";
            }

            var args = new StringBuilder();
            foreach (var parameter in method.Parameters)
            {
                var paramType = GetFriendlyTypeName(parameter.ParameterType);

                args.Append(paramType + " " + parameter.Name + ", ");
            }

            if (args.Length > 0)
            {
                args.Remove(args.Length - 2, 2);
            }

            return args.ToString();
        }

        private static string GetFriendlyTypeName(TypeReference type)
        {
            string fullName = type.FullName;


            Dictionary<string, string> typeNames = new Dictionary<string, string>
                {
                { "System.Byte", "byte" },
                { "System.SByte", "sbyte" },
                { "System.Int32", "int" },
                { "System.UInt32", "uint" },
                { "System.Int16", "short" },
                { "System.UInt16", "ushort" },
                { "System.Int64", "long" },
                { "System.UInt64", "ulong" },
                { "System.Single", "float" },
                { "System.Double", "double" },
                { "System.Boolean", "bool" },
                { "System.Char", "char" },
                { "System.Object", "object" },
                { "System.String", "string" },
                { "System.Decimal", "decimal" },
                { "System.Void", "void" }
                // U can extend the dict
            };

            if (typeNames.TryGetValue(fullName, out string friendlyName))
            {
                return friendlyName;
            }
            else
            {
                var typeName = type.Name;

                typeName = typeName.Replace("/", ".");

                var backTick = typeName.IndexOf('`');
                if (backTick > 0)
                {

                    typeName = typeName.Substring(0, backTick);

                    if (type is GenericInstanceType git)
                    {
                        typeName += "<";
                        foreach (var argument in git.GenericArguments)
                        {
                            typeName += GetFriendlyTypeName(argument) + ", ";
                        }
                        typeName = typeName.Remove(typeName.Length - 2, 2);
                        typeName += ">";
                    }
                }

                return typeName;
            }
        }


        private static string DetermineBindingFlags(MethodDefinition method)
        {
            var flags = new List<string>();

            if (method.IsPublic)
            {
                flags.Add("Public");
            }
            else
            {
                flags.Add("NonPublic");
            }

            if (method.IsStatic)
            {
                flags.Add("Static");
            }
            else
            {
                flags.Add("Instance");
            }

            return flags.Aggregate((current, next) => $"System.Reflection.BindingFlags.{current} | System.Reflection.BindingFlags.{next}");
        }

        private static void ProcessPInvokeMethods(string filePath, Action<MethodDefinition, PInvokeInfo> action)
        {
            try
            {
                var assembly = AssemblyDefinition.ReadAssembly(filePath);
                foreach (var module in assembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        foreach (var method in type.Methods)
                        {
                            if (method.IsPInvokeImpl)
                            {
                                var piInfo = method.PInvokeInfo;
                                action(method, piInfo);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    public class AssemblyFinder
    {
        public static void FindAssembly(string startDir, string methodName, bool recurse)
        {
            try
            {
                if (recurse)
                {
                    foreach (var dir in Directory.GetDirectories(startDir))
                    {
                        FindAssembly(dir, methodName, true);
                    }
                }

                var assemblyExtensions = new string[] { ".dll", ".exe" };

                foreach (var extension in assemblyExtensions)
                {
                    foreach (var file in Directory.GetFiles(startDir, "*" + extension))
                    {
                        if (Assembly.IsDotnetAssembly(file))
                        {

                            if (methodName == "")
                            {
                                Console.WriteLine("-------------");
                                Console.WriteLine($"[FILE] {file}");
                                Assembly.EnumeratePInvokeMethods(file);
                                Console.WriteLine("-------------");
                            }
                            else
                            {
                                Assembly.FindPInvokeMethod(file, methodName);
                            }

                        }
                    }
                }

            }
            catch { }
        }
    }

    internal class Program
    {
        static void ShowAwesomeBanner()
        {
            var art = @"

     . .  .  .  . . .
   .                  .                  _.-/`/`'-._
   . Nice assembly :D .                /_..--''''_-'
    .  .  .  .      .`                //-.__\_\.-'
                `..'  _\\\//  --.___ // ___.---.._
                  _- /@/@\  \       ||``          `-_
                .'  ,\_\_/   |    \_||_/      ,-._   `.
               ;   { o    /   }     ""        `-._`.   ;
              ;     `-==-'   /                    \_|   ;
             |        |>o<|  }@@@}                       |
             |       <(___<) }@@@@}                      |
             |       <(___<) }@@@@@}                     |
             |        <\___<) \_.?@@}                    |
              ;         V`--V`__./@}                    ;
               \      tx      ooo@}                    /
                \                                     /
                 `.                                 .'
                   `-._          Parasite Invoke_.-'
                       ``------'''''''''------``

";
            Console.WriteLine(art);
            Console.WriteLine("\t\tMichael Zhmaylo (github.com/MzHmO)");
        }
        static void Help()
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("[PARAMETER MANDATORY]\n \"--path <PATH>\", \"The start directory to list .NET assemblies from.\"");
            Console.WriteLine("\n[OPTIONAL PARAMS]\n \"-r|--recurse\", \"Recursively discover assemblies\"");
            Console.WriteLine("\"--method <METHOD>\", \"Name of the PInvoke method to find\"");
            Console.WriteLine("");
        }
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            ShowAwesomeBanner();
            var recurse = app.Option("-r|--recurse", "Recursively discover assemblies", CommandOptionType.NoValue);
            var path = app.Option("--path <PATH>", "The start directory to list .NET assemblies from.", CommandOptionType.SingleValue);
            var methodName = app.Option("--method <METHOD>", "Name of the PInvoke method to find", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (path.HasValue())
                {
                    if (Directory.Exists(path.Value()))
                    {
                        AssemblyFinder.FindAssembly(
                            path.Value(),
                            methodName.HasValue() ? methodName.Value() : "",
                            recurse.HasValue() ? true : false);
                    }
                    else
                    {
                        Console.WriteLine($"Path doesn't exists: {path}");
                        return 2;
                    }
                }
                else
                {
                    Help();
                    return 1;
                }

                return 0;
            });

            app.Execute(args);
            return 0;
        }
    }
}
