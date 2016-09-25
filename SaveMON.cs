/* Date: 31.10.2015, Time: 0:02 */
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace speakerconv
{
	public class SaveMON : OutputProcessor
	{
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			using(BinaryWriter writer = new BinaryWriter(new FileStream(file.Path, FileMode.Create), Encoding.ASCII))
			{
				writer.Write((byte)0x08);
				writer.Write("MONOTONE".ToCharArray());
				writer.Write(new byte[82]);
				writer.Write(new byte[]{1,1,1,2,0});
				writer.Write(Enumerable.Repeat((byte)0xFF, 255).ToArray());
				
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
							for(int i = 0; i < cmd.DelayValue*60/1000.0; i++)
							{
								writer.Write((byte)0);
								writer.Write(lastb);
							}
							break;
					}
				}
			}
		}
	}
}
