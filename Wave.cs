/* Date: 20.9.2015, Time: 23:37 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IllidanS4.Wave
{
	public class WavePlayer
	{
		private double vol;
		public double Volume{
			get{
				return vol;
			}
			set{
				if(value < 0 || value > 1) throw new ArgumentOutOfRangeException("value");
				vol = value;
			}
		}
		public int SampleRate{get; private set;}
		
		public WavePlayer()
		{
			Volume = 0.25;
			SampleRate = 44100;
		}
		
		public void PlayBeep(double frequency, int duration)
		{
			PlayWave(new Wave(frequency, duration));
		}
		
		public void PlayWave(IWaveSound wave)
		{
			var samples = CreateWave(wave);
	        using(var buffer = new MemoryStream())
	        {
	        	var writer = new WaveWriter();
				writer.WriteWave(buffer, samples);
			    buffer.Position = 0;
			    
			    var player = new SoundPlayer(buffer);
			    player.Play();
	        }
		}
		
		public short[] CreateWave(IWaveSound wave)
		{
			double rateDouble = SampleRate;
			var waveFunc = wave.ToFunction();
		    
			short[] samples = new short[(ulong)SampleRate * (ulong)wave.Duration / 1000];
	        double amp = Volume * Int16.MaxValue;
	        for(int i = 0; i < samples.Length; i++)
	        {
	        	double val = waveFunc[i / rateDouble];
	        	samples[i] = (short)(amp * val);
	        }
	        return samples;
		}
		
		public void PlayWave(WaveFunction wave, int duration)
		{
			PlayWave(new Wave(1, duration){Type = wave});
		}
	}
	
	public interface IWaveSound
	{
		WaveFunction ToFunction();
		int Duration{get;}
	}
	
	public class Wave : IWaveSound
	{
		public WaveFunction Type{get;set;}
		private double vol;
		public double Volume{
			get{
				return vol;
			}
			set{
				if(value < 0 || value > 1) throw new ArgumentOutOfRangeException("value");
				vol = value;
			}
		}
		public double Frequency{get;set;}
		public int Duration{get;set;}
		
		public Wave(double frequency, int duration) : this()
		{
			Frequency = frequency;
			Duration = duration;
			Type = WaveFunction.Sine;
			Volume = 1;
		}
		
		protected Wave()
		{
			asFunc = (Func<double,double>)(x => Volume*Type[Frequency*x]);
		}
		
		private readonly WaveFunction asFunc;
		public WaveFunction ToFunction()
		{
			return asFunc;
		}
	}
	
	public class WaveSong : IWaveSound
	{
		public List<Track> Waves{get; private set;}
		
		public WaveSong(bool singleChannel)
		{
			Waves = new List<Track>();
			Volume = 1;
			
			if(singleChannel)
			{
				Track currentWave = default(Track);
				asFunc = (Func<double,double>)(
					x => {
						double xms = x*1000;
						if(currentWave.Wave == null || !(currentWave.Start <= xms && currentWave.Start+currentWave.Wave.Duration > xms))
						{
							foreach(var wave in Waves)
							{
								if(wave.Start <= xms && wave.Start+wave.Wave.Duration > xms)
								{
									currentWave = wave;
									return currentWave.Wave.ToFunction()[x];
								}
							}
							return 0;
						}
						return currentWave.Wave.ToFunction()[x];
					}
				);
			}else{
				asFunc = (Func<double,double>)(
					x => {
						double val = 0;
						double xms = x*1000;
						foreach(var wave in Waves)
						{
							if(wave.Start <= xms && wave.Start+wave.Wave.Duration > xms)
							{
								val += wave.Wave.ToFunction()[x];
							}
						}
						return val;
					}
				);
			}
		}
		
		public short[] GetSamples(int sampleRate=44100)
		{
			double rateDouble = sampleRate;
		    
			short[] samples = new short[(ulong)sampleRate * (ulong)Duration / 1000];
	        double amp = Volume * Int16.MaxValue;
	        foreach(var wave in Waves)
	        {
	        	int sampleStart = (int)(wave.Start/1000.0*sampleRate);
	        	int sampleLength = (int)(wave.Wave.Duration/1000.0*sampleRate);
	        	var wfunc = wave.Wave.ToFunction();
	        	for(int i = 0; i < sampleLength; i++)
	        	{
	        		samples[sampleStart+i] += (short)(amp*wfunc[i / rateDouble]);
	        	}
	        }
	        return samples;
		}
		
		public WaveSong() : this(false)
		{
			
		}
		
		public void AddWave(int start, Wave wave)
		{
			Waves.Add(new Track(start, wave));
		}
		
		public int Duration{
			get{
				return Waves.Max(w => w.Start+w.Wave.Duration);
			}
		}
		
		private double vol;
		public double Volume{
			get{
				return vol;
			}
			set{
				if(value < 0 || value > 1) throw new ArgumentOutOfRangeException("value");
				vol = value;
			}
		}
		
		private readonly WaveFunction asFunc;
		public WaveFunction ToFunction()
		{
			return asFunc;
		}
		
		public struct Track
		{
			public int Start{get; private set;}
			public Wave Wave{get; private set;}
			
			public Track(int start, Wave wave) : this()
			{
				Start = start;
				Wave = wave;
			}
		}
	}
	
	public class WaveWriter
	{
		public int SampleRate{get;set;}
		
		private const int RIFF = 0x46464952;
		private const int WAVE = 0x45564157;
		private const int fmt  = 0x20746D66;
		private const int data = 0x61746164;
		
		public WaveWriter()
		{
			SampleRate = 44100;
		}
		
		public void WriteWave(Stream output, short[] samples)
		{
			var writer = WriteWaveBase(output, 1, 16, samples.Length);
			foreach(var sample in samples)
			{
				writer.Write(sample);
			}
		}
		
		private BinaryWriter WriteWaveBase(Stream output, short formatType, short bitsPerSample, int numSamples)
		{
			var writer = new BinaryWriter(output);
			
		    int formatChunkSize = 16;
		    int headerSize = 8;
		    short tracks = 1;
		    short frameSize = (short)(tracks * ((bitsPerSample + 7) / 8));
		    int bytesPerSecond = SampleRate * frameSize;
		    int waveSize = 4;
		    int dataChunkSize = numSamples * frameSize;
		    int fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
		    
		    writer.Write(RIFF);
		    writer.Write(fileSize);
		    writer.Write(WAVE);
		    writer.Write(fmt);
		    writer.Write(formatChunkSize);
		    writer.Write(formatType);
		    writer.Write(tracks);
		    writer.Write(SampleRate);
		    writer.Write(bytesPerSecond);
		    writer.Write(frameSize);
		    writer.Write(bitsPerSample);
		    writer.Write(data);
		    writer.Write(dataChunkSize);
			return writer;
		}
	}
	
	public class WaveFunction
	{
		public static readonly WaveFunction Sine = new PeriodicWave(x => Math.Sin(x*2*Math.PI));
		public static readonly WaveFunction Square = new PeriodicWave(
			x => {
				if(x > 0.5) return 1;
				else if(x < 0.5) return -1;
				else return 0;
			}
		);
		public static readonly WaveFunction Triangle = new PeriodicWave(
			x => {
				if(x < 0.25) return 4*x;
				else if(x < 0.75) return 2-4*x;
				else return 4*x-4;
			}
		);
		public static readonly WaveFunction Circle = new PeriodicWave(
			x => {
				if(x < 0.5) return Math.Sqrt(1.0-Math.Pow(4.0*x-1.0, 2));
				else return -Math.Sqrt(1.0-Math.Pow(4.0*x-3.0, 2));
			}
		);
		public static readonly WaveFunction AbsSine = new PeriodicWave(
			x => Math.Abs(Sine[x])
		);
		public static readonly WaveFunction HalfSine = new PeriodicWave(
			x => {
				if(x < 0.5) return Sine[x];
				else return 0;
			}
		);
		public static readonly WaveFunction HalfAbsSine = new PeriodicWave(
			x => {
				if(x%0.5 < 0.25) return AbsSine[x];
				else return 0;
			}
		);
		
		protected Func<double,double> Function{get; private set;}
		
		public virtual double this[double x]{
			get{
				return Function(x);
			}
		}
		
		public WaveFunction(Func<double,double> func)
		{
			Function = func;
		}
		
		public static WaveFunction operator +(WaveFunction a, WaveFunction b)
		{
			return a.Combine((x,y)=>x+y, b);
		}
		public static WaveFunction operator -(WaveFunction a, WaveFunction b)
		{
			return a.Combine((x,y)=>x-y, b);
		}
		public static WaveFunction operator *(WaveFunction a, WaveFunction b)
		{
			return a.Combine((x,y)=>x*y, b);
		}
		public static WaveFunction operator /(WaveFunction a, WaveFunction b)
		{
			return a.Combine((x,y)=>x/y, b);
		}
		public static WaveFunction operator %(WaveFunction a, WaveFunction b)
		{
			return a.Combine((x,y)=>x%y, b);
		}
		public static WaveFunction operator ^(WaveFunction a, WaveFunction b)
		{
			return a.Combine((x,y)=>Math.Pow(x, y), b);
		}
		public static WaveFunction operator |(WaveFunction a, WaveFunction b)
		{
			return a.Join(b);
		}
		
		protected virtual WaveFunction Combine(Func<double,double,double> func, WaveFunction wave)
		{
			return new WaveFunction(
				x => func(this[x], wave[x])
			);
		}
		protected virtual WaveFunction Join(WaveFunction wave)
		{
			return new WaveFunction(
				x => wave[this[x]]
			);
		}
		
		public static implicit operator WaveFunction(int val)
		{
			return new ConstantWave(val);
		}
		public static implicit operator WaveFunction(double val)
		{
			return new ConstantWave(val);
		}
		public static implicit operator WaveFunction(Func<double,double> func)
		{
			return new WaveFunction(func);
		}
	}
	
	public class PeriodicWave : WaveFunction
	{
		public override double this[double x]{
			get{
				x %= 1;
				if(x < 0) x += 1;
				return Function(x);
			}
		}
		
		public PeriodicWave(Func<double,double> func) : base(func)
		{
			
		}
		
		protected override WaveFunction Combine(Func<double, double, double> func, WaveFunction wave)
		{
			if(wave is PeriodicWave)
			{
				return new PeriodicWave(
					x => func(this[x], wave[x])
				);
			}else{
				return base.Combine(func, wave);
			}
		}
		protected override WaveFunction Join(WaveFunction wave)
		{
			if(wave is PeriodicWave)
			{
				return new PeriodicWave(
					x => wave[this[x]]
				);
			}else{
				return base.Join(wave);
			}
		}
	}
	
	public class ConstantWave : PeriodicWave
	{
		private readonly double value;
		
		public override double this[double x]{
			get{
				return value;
			}
		}
		
		public ConstantWave(double value) : base(x => value)
		{
			this.value = value;
		}
	}
}