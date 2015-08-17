/* Date: 9.12.2014, Time: 20:22 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace speakerconv
{
	public class LoadTXT : InputProcessor
	{
		public LoadTXT()
		{
			
		}
		
		public override IList<OutputFile> ProcessStream(Stream input, ConvertOptions options)
		{
			List<RPCCommand> rpc = new List<RPCCommand>();
			StreamReader reader = new StreamReader(input);
			string line;
			while((line = reader.ReadLine()) != null)
			{
				string[] split = line.Split(new[]{": "}, 0);
				RPCCommandType cmd = (RPCCommandType)Enum.Parse(typeof(RPCCommandType), split[0]);
				int value = Int32.Parse(split[1]);
				rpc.Add(new RPCCommand(cmd, value));
			}
			return LoadPCS.ProcessRPC(rpc, options);
		}
	}
}
