/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 17:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace speakerconv
{
	public class SaveDRO : OutputProcessor
	{
		private static readonly byte[] oper1 = new byte[]{0, 1, 2, 8, 9, 10, 16, 17, 18};
		
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			List<DROCommand> dro = new List<DROCommand>();
			if(options.MultiChannel)
			{
				for(int i = 0; i < 9; i++)
				{
					dro.Add(new DROCommand(0x20 + oper1[i], 0x01));
					dro.Add(new DROCommand(0x20 + oper1[i] + 3, 0x01));
					dro.Add(new DROCommand(0x40 + oper1[i], 0x10));
					dro.Add(new DROCommand(0x40 + oper1[i] + 3, 0x07));
					dro.Add(new DROCommand(0x60 + oper1[i], 0xF0));
					dro.Add(new DROCommand(0x60 + oper1[i] + 3, 0xF0));
					dro.Add(new DROCommand(0x80 + oper1[i], 0x77));
					dro.Add(new DROCommand(0x80 + oper1[i] + 3, 0x77));
					dro.Add(new DROCommand(0xE0 + oper1[i], options.DRO_Waveform ?? 2));
				}
			}else{
				dro.Add(new DROCommand(0x20, 0x01));
				dro.Add(new DROCommand(0x23, 0x01));
				dro.Add(new DROCommand(0x40, 0x10));
				dro.Add(new DROCommand(0x43, 0x07));
				dro.Add(new DROCommand(0x60, 0xF0));
				dro.Add(new DROCommand(0x63, 0xF0));
				dro.Add(new DROCommand(0x80, 0x77));
				dro.Add(new DROCommand(0x83, 0x77));
				dro.Add(new DROCommand(0xE0, options.DRO_Waveform ?? 2));
			}
			if(options.DRO_PrefixCommands != null) dro.AddRange(options.DRO_PrefixCommands);
			
			foreach(var cmd in file.Data)
			{
				if(cmd.Channel > 8)
				{
					throw new ArgumentException("Only 9 channels are supported.");
				}
				switch(cmd.Type)
				{
					case RPCCommandType.Delay:
						dro.AddRange(DROCommand.Delay(cmd.Data));
						break;
					case RPCCommandType.SetCountdown:
						double frequency = 1193180.0/cmd.Data;
						int octave = 4;
						while(frequency > 780.0375) //0x03FF * 0.7625
						{
							frequency /= 2;
							octave += 1;
							if(octave > 7) break;
						}
						if(octave > 7)
						{
							dro.Add(new DROCommand(0xB0 | cmd.Channel, 0x10));
						}else{
							int fnum = (int)(frequency/0.7625);
							dro.Add(new DROCommand(0xA0 | cmd.Channel,  fnum & 0x00FF));
							dro.Add(new DROCommand(0xB0 | cmd.Channel,((fnum & 0x0300) >> 8) | 0x20 | ((octave & 7) << 2)));
						}
						break;
					case RPCCommandType.ClearCountdown:
						dro.Add(new DROCommand(0xB0 | cmd.Channel, 0x10));
						break;
				}
			}
			
			if(options.DRO_EndDelay > 0) dro.AddRange(DROCommand.Delay(options.DRO_EndDelay));
			
			if(options.DRO_Optimize)
			{
				byte?[] registers = new byte?[0xFF];
				for(int i = 0; i < dro.Count; i++)
				{
					DROCommand cmd = dro[i];
					if(cmd.IsOPL)
					{
						if(registers[cmd.OPLRegister] == cmd.OPLValue)
						{
							dro.RemoveAt(i);
							i -= 1;
						}
						registers[cmd.OPLRegister] = cmd.OPLValue;
					}
				}
				for(int i = 0; i < dro.Count; i++)
				{
					DROCommand cmd = dro[i];
					if(cmd.IsDelay)
					{
						if(i+1 < dro.Count && dro[i+1].IsDelay)
						{
							int delay = cmd.DelayValue + dro[i+1].DelayValue;
							dro[i] = DROCommand.Delay(delay).First();
							dro.RemoveAt(i+1);
							i -= 1;
						}
					}
				}
			}
			
			int bytesize = dro.Sum(cmd => cmd.Length);
			int timesize = dro.Sum(cmd => cmd.DelayValue);
			using(BinaryWriter writer = new BinaryWriter(new FileStream(file.Path, FileMode.Create)))
			{
				writer.Write("DBRAWOPL".ToCharArray());
				writer.Write((short)0);
				writer.Write((short)1);
				writer.Write(timesize);
				writer.Write(bytesize);
				writer.Write((byte)0);
				writer.Write(new byte[3]);
				foreach(var cmd in dro)
				{
					writer.Write((byte)cmd.Register);
					writer.Write(cmd.Data);
				}
			}
		}
	}
}
