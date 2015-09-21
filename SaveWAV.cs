/* Date: 21.9.2015, Time: 14:48 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IllidanS4.Wave;

namespace speakerconv
{
	public class SaveWAV : OutputProcessor
	{
		public static readonly WavePlayer Player = new WavePlayer();
		public static readonly WaveWriter Writer = new WaveWriter();
		
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			using(var stream = new FileStream(file.Path, FileMode.Create))
			{
				Writer.WriteWave(stream, CreateSong(file.Data, GetWaveform(options)));
			}
		}
		
		public static WaveFunction GetWaveform(ConvertOptions options)
		{
			if(options.DRO_Waveform.HasValue)
			{
				switch(options.DRO_Waveform.Value)
				{
					case 0:
						return WaveFunction.Sine;
					case 1:
						return WaveFunction.HalfSine;
					case 2:
						return WaveFunction.AbsSine;
					case 3:
						return WaveFunction.HalfAbsSine;
					case 4:default:
						return WaveFunction.Square;
					case 5:
						return WaveFunction.Triangle;
					case 6:
						return WaveFunction.Circle;
				}
			}else{
				return WaveFunction.Square;
			}
		}
		
		public static short[] CreateSong(IList<RPCCommand> data, WaveFunction waveType)
		{
			var song = new WaveSong();
			song.Volume = 0.25;
			int time = 0;
			var channels = new Dictionary<int,WaveSong.Track>();
			foreach(var cmd in data)
			{
				WaveSong.Track playing;
				switch(cmd.Type)
				{
					case RPCCommandType.Delay:
						time += cmd.DelayValue;
						break;
					case RPCCommandType.SetCountdown:
						double freq = LoadMDT.CountdownToFrequency(cmd.Data);
						if(!channels.TryGetValue(cmd.Channel, out playing) || playing.Wave.Frequency != freq)
						{
							if(playing.Wave != null)
							{
								playing.Wave.Duration = time-playing.Start;
							}
							playing = new WaveSong.Track(time, new Wave(freq, 0){Type = waveType});
							song.Waves.Add(playing);
							channels[cmd.Channel] = playing;
						}
						break;
					case RPCCommandType.ClearCountdown:
						if(channels.TryGetValue(cmd.Channel, out playing))
						{
							if(playing.Wave != null)
							{
								playing.Wave.Duration = time-playing.Start;
							}
							channels.Remove(cmd.Channel);
						}
						break;
				}
			}
			foreach(WaveSong.Track playing in channels.Values)
			{
				playing.Wave.Duration = time-playing.Start;
			}
			return song.GetSamples();
		}
	}
}
