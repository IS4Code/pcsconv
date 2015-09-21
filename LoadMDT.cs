/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 7.12.2013
 * Time: 15:20
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace speakerconv
{
	public class LoadMDT : InputProcessor //Miditones
	{
		public LoadMDT()
		{
			
		}
		
		public override IList<OutputFile> ProcessStream(Stream input, ConvertOptions options)
		{
			List<RPCCommand> rpc = new List<RPCCommand>();
			
			Channel[] channels = new Channel[16];
			int lastchannel = -1;
			int time = 0;
			
			using(BinaryReader reader = new BinaryReader(input))
			{
				try{
					if(options.MultiChannel)
					{
						while(true)
						{
							byte command = reader.ReadByte();
							if((command & 0x80) != 0)
							{
								switch(command & 0xF0)
								{
									case 0x90: //Play
										int channel = command & 0x0F;
										int note = reader.ReadByte();
										double freq = NoteToFrequency(note);
										rpc.Add(new RPCCommand(RPCCommandType.SetCountdown, channel, FrequencyToCountdown(freq)));
										break;
									case 0x80: //Stop
										channel = command & 0x0F;
										rpc.Add(RPCCommand.ClearCountdown(channel));
										break;
									case 0xF0: case 0xE0: //End
										throw new EndOfStreamException();
								}
							}else{ //Delay
								byte next = reader.ReadByte();
								int delay = (command<<8)|next;
								time += delay;
								rpc.Add(RPCCommand.Delay(delay));
							}
						}
					}else{
						while(true)
						{
							byte command = reader.ReadByte();
							if((command & 0x80) != 0)
							{
								switch(command & 0xF0)
								{
									case 0x90: //Play
										int channel = command & 0x0F;
										lastchannel = channel;
										int note = reader.ReadByte();
										channels[channel].Frequency = NoteToFrequency(note);
										channels[channel].StartTime = time;
										double freq = FinalFrequency(channels);
										rpc.Add(new RPCCommand(RPCCommandType.SetCountdown, FrequencyToCountdown(freq)));
										break;
									case 0x80: //Stop
										channel = command & 0x0F;
										channels[channel].Frequency = 0;
										if(channels.All(p => p.Frequency == 0))
										{
											rpc.Add(RPCCommand.ClearCountdown());
										}else{
											freq = FinalFrequency(channels);
											rpc.Add(new RPCCommand(RPCCommandType.SetCountdown, FrequencyToCountdown(freq)));
										}
										break;
									case 0xF0: case 0xE0: //End
										throw new EndOfStreamException();
								}
							}else{ //Delay
								byte next = reader.ReadByte();
								int delay = (command<<8)|next;
								time += delay;
								rpc.Add(RPCCommand.Delay(delay));
							}
						}
					}
				}catch(EndOfStreamException)
				{
					
				}
			}
			
			return new[]{new OutputFile(options.OutputPath, rpc)};
		}
		
		private static int FinalNote(int[] channels)
		{
			///*average value*/int note = (int)Math.Round(playing.Where(p => p != 0).Select(p => p-1).Average());
			/*maximum value*/int note = channels.Max()-1;
			///*geometric mean*/int note = (int)Math.Round(Math.Pow(channels.Where(p => p != 0).Select(p => p-1).Aggregate(1, (acc,val) => acc * val), 1d/channels.Where(p => p != 0).Count()));
			return note;
		}
		
		private static double FinalFrequency(Channel[] channels)
		{
			var normal = channels.Where(ch => ch.Frequency > 0);
			//return MaximumFrequencyMode(normal);
			return LastFrequencyMode(normal);
		}
		
		private static double MaximumFrequencyMode(IEnumerable<Channel> normal)
		{
			return normal.Max(ch => ch.Frequency);
		}
		
		private static double LastFrequencyMode(IEnumerable<Channel> normal)
		{
			Channel selected = default(Channel);
			foreach(var channel in normal)
			{
				if(
					(channel.StartTime > selected.StartTime) ||
					(channel.StartTime == selected.StartTime && channel.Frequency > selected.Frequency)
				)
				{
					selected = channel;
				}
			}
			return selected.Frequency;
		}
		
		private static double LastComparativeFrequencyMode(IEnumerable<Channel> normal)
		{
			Channel selected = default(Channel);
			foreach(var channel in normal)
			{
				if(
					(channel.StartTime > selected.StartTime && channel.Frequency > selected.Frequency*2) ||
					(channel.StartTime == selected.StartTime && channel.Frequency > selected.Frequency)
				)
				{
					selected = channel;
				}
			}
			return selected.Frequency;
		}
		
		/*
		//Awful
		private static double HarmonicFrequencyMode(IEnumerable<Channel> normal)
		{
			return normal.Count()/normal.Average(ch => 1/ch.Frequency);
		}
		
		private static double QuadraticFrequencyMode(IEnumerable<Channel> normal)
		{
			return Math.Sqrt(normal.Average(ch => ch.Frequency*ch.Frequency));
		}*/
		
		private static double NoteToFrequency(int note)
		{
			return Math.Pow(2, (note-69)/12d)*440;
		}
		
		public static int FrequencyToCountdown(double freq)
		{
			return (int)Math.Round(1193180/freq);
		}
		
		public static double CountdownToFrequency(int cd)
		{
			return Math.Round(1193180.0/cd);
		}
		
		private static int NoteToCountdown(int note)
		{
			return FrequencyToCountdown(NoteToFrequency(note));
		}
		
		struct Channel
		{
			public double Frequency;
			public int StartTime;
		}
	}
}
