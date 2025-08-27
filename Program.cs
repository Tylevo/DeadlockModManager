using Deadlock_Mod_Loader2;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DeadlockModManager   // ← your namespace
{
    internal static class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string LibsDir = Path.Combine(BaseDir, "libs");
        private static readonly string ToolsDir = Path.Combine(LibsDir, "tools");
        private static readonly string LogPath = Path.Combine(BaseDir, "loader.log");

        [STAThread]
        private static void Main()
        {
            // 0) Minimal logging so we can verify this is running
            SafeLog($"=== Loader start {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            SafeLog($"BaseDir={BaseDir}");
            SafeLog($"LibsDir exists? {Directory.Exists(LibsDir)}");
            SafeLog($"Newtonsoft at libs? {File.Exists(Path.Combine(LibsDir, "Newtonsoft.Json.dll"))}");

            // 1) Old but effective: add private bin path (like <probing> but in code)
#pragma warning disable 618
            try { AppDomain.CurrentDomain.AppendPrivatePath("libs;libs\\tools"); } catch { }
#pragma warning restore 618

            // 2) Native search path (if you ever add native DLLs)
            try { SetDllDirectory(LibsDir); } catch { }

            // 3) Managed: last-chance resolver from libs/ and libs/tools
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var req = new AssemblyName(args.Name);
                var file = req.Name + ".dll";
                string[] candidates =
                {
                    Path.Combine(BaseDir, file),
                    Path.Combine(LibsDir, file),
                    Path.Combine(ToolsDir, file),
                };

                foreach (var c in candidates)
                {
                    if (File.Exists(c))
                    {
                        SafeLog($"Resolve {req} -> {c}");
                        // LoadFrom is fine for plugin-style load
                        return Assembly.LoadFrom(c);
                    }
                }

                SafeLog($"Resolve MISS {req}");
                return null;
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Sanity ping so we know this Main ran
            SafeLog("Starting Form1()");
            Application.Run(new Form1()); // ← your startup form
        }

        private static void SafeLog(string line)
        {
            try { File.AppendAllText(LogPath, line + Environment.NewLine); } catch { }
        }
    }
}
