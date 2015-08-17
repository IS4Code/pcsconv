/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 18:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace speakerconv
{
	public class PlayBeep : OutputProcessor
	{
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			Console.WriteLine(file.Path);
			Console.WriteLine(TimeSpan.FromMilliseconds(file.Data.Sum(cmd => cmd.DelayValue)));
			Stopwatch sw = new Stopwatch();
			var rpc = file.Data;
			sw.Start();
			for(int i = 0; i < rpc.Count; i++)
			{
				var cmd = rpc[i];
				if(cmd.Type == RPCCommandType.SetCountdown || cmd.Type == RPCCommandType.ClearCountdown)
				{
					int delay = 0;
					for(int j = i+1; j < rpc.Count; j++)
					{
						var cmd2 = rpc[j];
						if(cmd2.Type == RPCCommandType.Delay)
						{
							delay += cmd2.Data;
						}else{
							i = j-1;
							break;
						}
					}
					if(cmd.Type == RPCCommandType.SetCountdown)
					{
						int freq = 1193180/cmd.Data;
						if(freq >= 37 && freq <= 32767 && delay > 0)
						{
							Console.Beep(freq, delay);
						}else if(delay > 0)
						{
							Console.WriteLine("Bad frequency "+freq);
							Thread.Sleep(delay);
						}
					}else if(cmd.Type == RPCCommandType.ClearCountdown)
					{
						Thread.Sleep(delay);
					}
				}else if(cmd.Type == RPCCommandType.Delay)
				{
					Thread.Sleep(cmd.Data);
				}
				Console.Write(sw.Elapsed+"\r");
			}
			sw.Stop();
			Console.WriteLine();
		}
	}
}
