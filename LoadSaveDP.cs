/* Date: 19.9.2015, Time: 12:50 */
using System;
using System.Collections.Generic;
using System.IO;

namespace speakerconv
{
	public class LoadSaveDP : IInputProcessor, IOutputProcessor //id DP
	{
		static readonly double[] frequencies = {
		    0, 175.00, 180.02, 185.01, 190.02, 196.02, 202.02, 208.01, 214.02, 220.02,
		    226.02, 233.04, 240.02, 247.03, 254.03, 262.00, 269.03, 277.03, 285.04,
		    294.03, 302.07, 311.04, 320.05, 330.06, 339.06, 349.08, 359.06, 370.09,
		    381.08, 392.10, 403.10, 415.01, 427.05, 440.12, 453.16, 466.08, 480.15,
		    494.07, 508.16, 523.09, 539.16, 554.19, 571.17, 587.19, 604.14, 622.09,
		    640.11, 659.21, 679.10, 698.17, 719.21, 740.18, 762.41, 784.47, 807.29,
		    831.48, 855.32, 880.57, 906.67, 932.17, 960.69, 988.55, 1017.20, 1046.64,
		    1077.85, 1109.93, 1141.79, 1175.54, 1210.12, 1244.19, 1281.61, 1318.43,
		    1357.42, 1397.16, 1439.30, 1480.37, 1523.85, 1569.97, 1614.58, 1661.81,
		    1711.87, 1762.45, 1813.34, 1864.34, 1921.38, 1975.46, 2036.14, 2093.29,
		    2157.64, 2217.80, 2285.78, 2353.41, 2420.24, 2490.98, 2565.97, 2639.77,
		};
		
		public LoadSaveDP()
		{
			
		}
		
		public virtual IList<OutputFile> ProcessStream(Stream input, ConvertOptions options)
		{
			List<RPCCommand> rpc = new List<RPCCommand>();
			
			using(BinaryReader reader = new BinaryReader(input))
			{
				reader.ReadInt16();
				int length = reader.ReadInt16();
				byte lastb = 0;
				int delay = 0;
				for(int i = 0; i < length; i++)
				{
					byte b = reader.ReadByte();
					if(b != lastb)
					{
						rpc.Add(RPCCommand.Delay(delay*1000/140));
						delay = 0;
					}
					if(b == 0)
					{
						rpc.Add(RPCCommand.ClearCountdown());
					}else{
						double freq = b*15;//frequencies[b];
						rpc.Add(RPCCommand.SetCountdown(LoadMDT.FrequencyToCountdown(freq)));
					}
					delay += 1;
					lastb = b;
				}
				rpc.Add(RPCCommand.Delay(delay*1000/140));
			}
			return LoadPCS.ProcessRPC(rpc, options);
		}
		
		public virtual void ProcessFile(OutputFile file, ConvertOptions options)
		{
			using(var stream = new FileStream(file.Path, FileMode.Create))
			{
				var writer = new BinaryWriter(stream);
				writer.Write(0);
				byte lastb = 0;
				foreach(var cmd in file.Data)
				{
					switch(cmd.Type)
					{
						case RPCCommandType.SetCountdown:
							double freq = LoadMDT.CountdownToFrequency(cmd.Data);
							var bval = freq/15;
							if(bval > 255)
							{
								lastb = 0;
							}else{
								lastb = (byte)bval;
							}
							break;
						case RPCCommandType.ClearCountdown:
							lastb = 0;
							break;
						case RPCCommandType.Delay:
							for(int i = 0; i < cmd.DelayValue*140/1000.0; i++)
							{
								writer.Write(lastb);
							}
							break;
					}
				}
				stream.Position = 2;
				writer.Write((short)(stream.Length-4));
			}
		}
		
		public virtual IList<OutputFile> ProcessFile(string file, ConvertOptions options)
		{
			using(FileStream stream = new FileStream(file, FileMode.Open))
			{
				return ProcessStream(stream, options);
			}
		}
		
		public virtual void ProcessFiles(IEnumerable<OutputFile> files, ConvertOptions options)
		{
			foreach(var file in files) ProcessFile(file, options);
		}
	}
}
