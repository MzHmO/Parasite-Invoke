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
