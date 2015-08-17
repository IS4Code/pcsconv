/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 18:22
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace speakerconv
{
	public class PlayDRO : OutputProcessor
	{
		private static readonly SaveDRO save = new SaveDRO();
		private static readonly ProcessStartInfo dro_player = new ProcessStartInfo("cmd")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		
		public PlayDRO()
		{
			
		}
		
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			file = new OutputFile(Path.GetTempFileName(), file.Data);
			save.ProcessFile(file, options);
			dro_player.Arguments = "/c dro_player \""+file.Path+"\"";
			Process proc = Process.Start(dro_player);
			Process ctlProc = proc;
			ConsoleCancelEventHandler oncancel = (o,a) => {a.Cancel = true; ctlProc.Kill();};
			Console.CancelKeyPress += oncancel;
			try{
				int read;
				bool first = true;
				while((read = proc.StandardOutput.Read()) != -1)
				{
					if(first)
					{
						ctlProc = GetChildProcesses(proc).FirstOrDefault() ?? proc;
						first = false;
					}
					Console.Write((char)read);
				}
				proc.WaitForExit();
			}finally{
				Console.CancelKeyPress -= oncancel;
				if(!proc.HasExited) proc.Kill();
				File.Delete(file.Path);
			}
			Console.WriteLine();
		}
		
	    private static IEnumerable<Process> GetChildProcesses(Process process)
	    {
	        ManagementObjectSearcher mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));
			return mos.Get().Cast<ManagementObject>().Select(mo => Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
	        
	        /*foreach(ManagementObject mo in mos.Get())
	        {
	            children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
	        }
	
	        return children;*/
	    }
	}
}
