using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace sudo {

	class Program {
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		static extern bool FreeConsole();

		static void Main(string[] args) {
			if(args.Length == 0) {
				Console.Error.WriteLine("usage: sudo <cmd>");
				Environment.Exit(1);
			}

			if(args[0] == "-do") {
				var exit_code = Do(args[1], args[2], string.Join(" ", args.Skip(3)));
				Environment.Exit(exit_code);
				return;
			}

			var pid = Process.GetCurrentProcess().Id;
			var sudo_exe = Assembly.GetExecutingAssembly().Location;
			var pwd = Environment.CurrentDirectory;

			var p = new Process();
			p.StartInfo.FileName = "cmd.exe";
			p.StartInfo.Arguments = "/s /c \"\"" + sudo_exe + "\" -do \"" + pwd + "\" " + pid + " " + string.Join(" ", args) + "\"";
			p.StartInfo.Verb = "runas";
			p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			try {
				p.Start();
			} catch(Win32Exception) { Environment.Exit(1); } // user didn't provide consent
			
			p.WaitForExit();
			Environment.Exit(p.ExitCode);
		}

		static int Do(string dir, string parent_pid, string cmd) {
			uint pid;
			if(!uint.TryParse(parent_pid, out pid)) {
				Console.WriteLine("Couldn't get pid"); return 1;
			}

			FreeConsole();
			AttachConsole(pid);

			var p = new Process();
			var start = p.StartInfo;
			start.FileName = "powershell.exe";
			start.Arguments = "-noprofile -nologo " + cmd + "\nexit $lastexitcode";
			start.UseShellExecute = false;
			start.WorkingDirectory = dir;

			p.Start();
			p.WaitForExit();
			return p.ExitCode;
		}
	}
}
