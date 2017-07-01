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
				Writer.SampleRate = options.Wave_Frequency??44100;
				Writer.WriteWave(stream, CreateSong(file.Data, GetWaveform(options), options.Wave_Volume??1.0, options.Wave_Clip??false, options.Wave_Frequency??44100, options.ClickLength, options.AutoTemper));
			}
		}
		
		public static WaveFunction GetWaveform(ConvertOptions options)
		{
			if(options.Waveform.HasValue)
			{
				switch(options.Waveform.Value)
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
					case 7:
						return WaveFunction.AbsCircle;
					case 8:
						return WaveFunction.Sawtooth;
					case 9:
						return WaveFunction.Clausen;
					case 10:
						return WaveFunction.SineDouble;
				}
			}else{
				return WaveFunction.Square;
			}
		}
		
		public static short[] CreateSong(IList<RPCCommand> data, WaveFunction waveType, double volume, bool clip, int frequency, double? clickLength, bool temper)
		{
			var song = new WaveSong();
			song.NoClipping = !clip;
			song.Volume = volume;
			
			
			bool informed = false;
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
						if(temper)
						{
							freq = TemperFrequency(freq);
						}
						if(!channels.TryGetValue(cmd.Channel, out playing) || playing.Wave.Frequency != freq)
						{
							double phase = 0;
							if(playing.Wave != null)
							{
								playing.Wave.Duration = time-playing.Start;
								phase = playing.Wave.RemainingPhaseShift;
							}
							playing = new WaveSong.Track(time, new Wave(freq, 0){Type = waveType, PhaseShiftCoef = phase});
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
								if(playing.Wave.Duration == 0)
								{
									if(clickLength != null)
									{
										playing.Wave.Duration = 1000*clickLength.Value/playing.Wave.Frequency;
									}else if(!informed)
									{
										Console.WriteLine("Song contains zero-length waves. Use --clicks 0.5 to render them as clicks.");
										informed = true;
									}
								}
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
			return song.GetSamples<short>(frequency);
		}
		
		const double noteBase = 1.0594630943592952645618252949463;
		public static double TemperFrequency(double freq)
		{
			double note = Math.Log(freq/440, noteBase)+49;
			return Math.Pow(noteBase, Math.Round(note)-49)*440;
		}
	}
}
