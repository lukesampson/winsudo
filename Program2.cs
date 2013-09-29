using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace sudo {

	class Program2 {
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		static extern bool FreeConsole();

		static void Do(string dir, string parent_pid, string cmd) {
			Console.WriteLine("this is the elevated console");
			Console.WriteLine("dir: " + dir);
			Console.WriteLine("cmd: " + cmd);
			uint pid;
			if(!uint.TryParse(parent_pid, out pid)) {
				Console.WriteLine("Couldn't get pid"); return;
			}

			var freed = FreeConsole();
			var res = AttachConsole(pid);
			Console.WriteLine("Freed? " + freed + ", attached? " + res);

			var p = new Process();
			var start = p.StartInfo;
			start.FileName = "powershell.exe";
			start.Arguments = "-noprofile -nologo " + cmd + "\nexit $lastexitcode";
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;
			start.RedirectStandardError = true;
			start.RedirectStandardInput = true;
			start.WorkingDirectory = dir;

			p.Start();
			p.WaitForExit();
			return;			
		}

		static void Main(string[] args) {
			if(args.Length == 0) {
				Console.Error.WriteLine("usage: sudo <cmd>");
				Environment.Exit(1);
			}

			if(args[0] == "-do") {
				Do(args[1], args[2], string.Join(" ", args.Skip(3)));
				return;
			}

			Console.Write("Pause. Press any key to begin...");
			Console.ReadKey();

			var pid = Process.GetCurrentProcess().Id;
			var exe = Assembly.GetExecutingAssembly().Location;
			var pwd = Environment.CurrentDirectory;
			var exit_code = 0;

			var p = new Process();
			p.StartInfo.FileName = "powershell.exe";
			p.StartInfo.Arguments = "-noprofile -nologo & '" + exe + "' -do " + pwd + " " + pid + " " + string.Join(" ", args);
			p.StartInfo.Verb = "runas";

			p.Start();
			p.WaitForExit();

			Console.Write("su-done. Press any key to exit...");
			Console.ReadKey();
		}
	}
}
