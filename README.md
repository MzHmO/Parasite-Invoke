# Parasite-Invoke
Hide your P/Invoke signatures through other people's assemblies!

## Usage
![изображение](https://github.com/MzHmO/Parasite-Invoke/assets/92790655/7932c49f-232e-4184-8059-d107f3470f2e)

```shell
[PARAMETER MANDATORY]
 "--path <PATH>", "The start directory to list .NET assemblies from."

[OPTIONAL PARAMS]
 "-r|--recurse", "Recursively discover assemblies"
"--method <METHOD>", "Name of the PInvoke method to find"
```

The tool accepts one mandatory parameter, it is path. If you simply specify a `--path` (For ex, `--path C:\`), the tool will find all .NET assemblies on that path and output the P/Invoke signatures used in them, which you can use in your code to hide the use of P/Invoke (see `Example` below). To perform a recursive search for assemblies, add the `-r` parameter.

![изображение](https://github.com/MzHmO/Parasite-Invoke/assets/92790655/74bc4b69-cc38-493a-8ac2-1132f597e9b1)

But most likely you will be interested in hiding a particular PInvoke method. That's why I created the `--method` argument. You can use it to find .NET builds that have this method signature.


![изображение](https://github.com/MzHmO/Parasite-Invoke/assets/92790655/0a44ddda-790e-4686-b39b-598cf101201f)

Let's go to an example

## Example (u should go here)
Suppose you want to hide the use of the `VirtualAlloc()` function. You run my tool and receive the following output:
```shell
.\ParasiteInvoke.exe --path C:\ -r --method VirtualAlloc
```
![изображение](https://github.com/MzHmO/Parasite-Invoke/assets/92790655/09264552-c58b-4fee-a4d6-ee6ecb7f8b46)

You should just copy the signature into your code, then add arguments to call the method and quietly PARASITE on the PInvoke signature from someone else's .NET assembly.
```cs
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Template
{
    class Program
    {
        static void Main()
        {
            Assembly asm = Assembly.LoadFrom(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationClientsideProviders.dll");
            Type t = asm.GetType("MS.Win32.UnsafeNativeMethods", true);
            var methodInfo = t.GetMethod("VirtualAlloc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            IntPtr result = (System.IntPtr)methodInfo.Invoke(null, new object[] { IntPtr.Zero, new UIntPtr(10), 0x3000, 0x40 } );
            Marshal.Copy(new byte[] { 1, 2, 3 }, 0, result, 3);
            Console.WriteLine(result);
            return;
        }

    }
}
```

![изображение](https://github.com/MzHmO/Parasite-Invoke/assets/92790655/9c9b2cca-0b35-4df2-821f-f12aa7d68163)

Successfully invoke the function:
![изображение](https://github.com/MzHmO/Parasite-Invoke/assets/92790655/7a8c04c2-3239-464f-9f62-17507fc8fe7d)


## Example output
### Discover all .NET assemblies from C:\Windows\System32 directory with PInvoke Signatures
https://pastebin.com/9JyjcMAH

### Discover all .NET assemblies from C:\ with PInvoke signature of VirtualAlloc Method
https://pastebin.com/iBeTbXCw
