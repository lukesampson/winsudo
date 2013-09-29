using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace sudo {
	class Program {

		static void Do(string dir, string parent_pid, string cmd) {
			Console.WriteLine("/tmp/sudo/" + parent_pid);
			using(var c = new NamedPipeClientStream(".", "/tmp/sudo/" + parent_pid,
				PipeDirection.InOut, PipeOptions.Asynchronous,
				System.Security.Principal.TokenImpersonationLevel.Anonymous)) {

				c.Connect();
				var sw = new StreamWriter(c);
				sw.AutoFlush = true;

				var testinput = "one\r\n\two\r\nthree";
				Console.SetOut(sw);
				Console.SetIn(new StringReader(testinput));
				Console.WriteLine("test");

				//var sr = new StreamReader(Console.OpenStandardOutput());

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

				var writeout = Task.Run(() => {
					int outc;
					while(true) {
						try { outc = p.StandardOutput.Read(); } catch { break; }
						if(outc != -1) {
							sw.Write((char)outc);
							sw.Flush();
						}
					}
					Console.WriteLine("writeout end");
				});

				p.WaitForExit();
				sw.Close();
			}
		}

		static void OldMain(string[] args) {
			if(args.Length == 0) {
				Console.Error.WriteLine("usage: sudo <cmd>");
				Environment.Exit(1);
			}

			if(args[0] == "-do") {
				Do(args[1], args[2], string.Join(" ", args.Skip(3)));
				return;
			}

			var pid = Process.GetCurrentProcess().Id;
			var exe = Assembly.GetExecutingAssembly().Location;
			var pwd = Environment.CurrentDirectory;
			var exit_code = 0;

			using(var s = new NamedPipeServerStream("/tmp/sudo/" + pid, PipeDirection.InOut)) {
				var p = new Process();
				p.StartInfo.FileName = "powershell.exe";
				p.StartInfo.Arguments = "-noprofile -noexit -nologo & '" + exe + "' -do " + pwd + " " + pid + " " + string.Join(" ", args);
				p.StartInfo.Verb = "runas";
				p.Start();

				s.WaitForConnection();

				var sr = new StreamReader(s);

				Task.Run(() => {
					string line;
					while(true) {
						try { line = sr.ReadLine(); } catch { break; }
						if(line != null) Console.WriteLine(line);
					}
				});
				

				p.WaitForExit();
			}

			Console.Write("su-done. Press any key to exit...");
			Console.ReadKey();

			//Environment.Exit(exit_code);

			/*
			var read = Task.Run(() => Console.WriteLine("You said: " + Console.ReadLine()));

			var write = Task.Run(() => {
				for(var i = 0; i < 10; i++) {
					Console.WriteLine("it's " + i);
					System.Threading.Thread.Sleep(1000);
				}
			});

			Task.WaitAll(read, write);*/

			/*
			var p = new Process();
			var si = p.StartInfo;
			//si.Verb = "runas";
			si.FileName = "powershell.exe";
			si.Arguments = "-noprofile -nologo write-host 'hi!';read-host 'enter your name';";
			si.UseShellExecute = false;

			si.RedirectStandardOutput = true;
			si.RedirectStandardInput = true;

			p.Start();
			
			var readbuf = new char[1];

			var write = Task.Run(() => {
				int c;
				while(true) {
					c = p.StandardOutput.Read();
					if(c == -1) break;
					Console.Write((char)c);
				}
			});

			var exit = Task.Run(() => p.WaitForExit());

			Task.WaitAll(write, exit);
			 * */
		}
	}
}
