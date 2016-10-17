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
		public int SampleRate{get; set;}
		
		public WavePlayer()
		{
			Volume = 0.25;
			SampleRate = 44100;
		}
		
		public static void Play(ISound wave)
		{
			var player = new WavePlayer();
			player.PlayWave(wave, false);
		}
		
		public static void PlaySync(ISound wave)
		{
			var player = new WavePlayer();
			player.PlayWave(wave, true);
		}
		
		public void PlayBeep(double frequency, int duration)
		{
			PlayWave(new Wave(frequency, duration));
		}
		
		public void PlayWave(ISound wave, bool sync=false)
		{
			var samples = CreateWave<short>(wave);
	        using(var buffer = new MemoryStream())
	        {
	        	var writer = new WaveWriter();
	        	writer.SampleRate = SampleRate;
				writer.WriteWave(buffer, samples);
			    buffer.Position = 0;
			    
			    var player = new SoundPlayer(buffer);
			    if(sync)
			    {
			    	player.PlaySync();
			    }else{
			    	player.Play();
			    }
	        }
		}
		
		private class WaveCreator : WaveBase
		{
			public IWaveSound Wave{get; private set;}
			
			public WaveCreator(double volume, IWaveSound wave)
			{
				Volume = volume;
				Wave = wave;
			}
			
			protected override void WriteSamples(int sampleRate, double[] samples)
			{
				double rateDouble = sampleRate;
				var waveFunc = Wave.ToFunction();
		        for(int i = 0; i < samples.Length; i++)
		        {
		        	samples[i] = Volume * waveFunc[i / rateDouble] / waveFunc.Amplitude;
		        }
			}
			
			public override WaveFunction ToFunction()
			{
				return Volume * Wave.ToFunction();
			}
			
			protected override double GetDuration()
			{
				return Wave.Duration;
			}
		}
		
		public T[] CreateWave<T>(ISound wave)
		{
			var asSampled = wave as ISampledSound;
			if(asSampled != null) return (T[])asSampled.GetSamples(SampleRate, Type.GetTypeCode(typeof(T)));
			
			var asWave = wave as IWaveSound;
			if(asWave != null) return new WaveCreator(Volume, asWave).GetSamples<T>(SampleRate);
			
			throw new ArgumentException();
		}
		
		public void PlayWave(WaveFunction wave, int duration)
		{
			PlayWave(new Wave(1, duration){Type = wave});
		}
	}
	
	public interface ISound
	{
		double Duration{get;}
	}
	
	public interface IWaveSound : ISound
	{
		WaveFunction ToFunction();
	}
	
	public interface ISampledSound : ISound
	{
		Array GetSamples(int sampleRate=44100, TypeCode format=TypeCode.Int16);
	}
	
	public class Wave : WaveBase
	{
		public WaveFunction Type{get;set;}
		public double Frequency{get;set;}
		public double PhaseShiftCoef{get;set;}
		public new double Duration{get;set;}
		public WaveFunction FadeIn{get;set;}
		public double FadeInDuration{get;set;}
		public WaveFunction FadeOut{get;set;}
		public double FadeOutDuration{get;set;}
		
		public double RemainingPhaseShift{
			get{
				return (Duration*Frequency/1000)%1+PhaseShiftCoef;
			}
		}
		
		protected override double GetDuration()
		{
			return Duration;
		}
		
		public Wave(double frequency, double duration) : this()
		{
			Frequency = frequency;
			Duration = duration;
		}
		
		public Wave(double frequency, double duration, WaveFunction type) : this(frequency, duration)
		{
			Type = type;
		}
		
		protected Wave()
		{
			Type = WaveFunction.Sine;
			Volume = 1;
		}
		
		public override WaveFunction ToFunction()
		{
			return new WaveFunction(WaveFunc, Volume*Type.Amplitude);
		}
		
		private double WaveFunc(double x)
		{
			double y = Volume*Type[x*Frequency+PhaseShiftCoef];
			x *= 1000;
			if(FadeIn != null)
			{
				if(x <= FadeInDuration)
				{
					y *= Math.Abs(FadeIn[x/FadeInDuration/4]);
				}
			}
			if(FadeOut != null)
			{
				if(x >= Duration-FadeOutDuration)
				{
					y *= Math.Abs(FadeOut[(Duration-x)/FadeOutDuration/4]);
				}
			}
			return y;
		}
		
		protected override void WriteSamples(int sampleRate, double[] samples)
		{
			double rateDouble = sampleRate;
	        var waveFunc = ToFunction();
	        for(int i = 0; i < samples.Length; i++)
	        {
	        	samples[i] = Volume * waveFunc[i / rateDouble] / waveFunc.Amplitude;
	        }
		}
	}
	
	public abstract class SampledBase : ISampledSound
	{
		protected abstract double[] GetSamplesDouble(int sampleRate);
		
		public abstract double Duration{
			get;
		}
		
		private byte[] GetSamplesByte(int sampleRate)
		{
			double[] samples = GetSamplesDouble(sampleRate);
			byte[] samplesArray = new byte[samples.Length];
			for(long i = 0; i < samples.LongLength; i++)
			{
				double s = samples[i];
				var b = (byte)Center(samples[i], Byte.MinValue, Byte.MaxValue);
				samplesArray[i] = (byte)Center(samples[i], Byte.MinValue, Byte.MaxValue);
			}
	        return samplesArray;
		}
		
		private short[] GetSamplesInt16(int sampleRate)
		{
			double[] samples = GetSamplesDouble(sampleRate);
			short[] samplesArray = new short[samples.Length];
			for(long i = 0; i < samples.LongLength; i++)
			{
				samplesArray[i] = (short)Center(samples[i], Int16.MinValue, Int16.MaxValue);
			}
	        return samplesArray;
		}
		
		private int[] GetSamplesInt32(int sampleRate)
		{
			double[] samples = GetSamplesDouble(sampleRate);
			int[] samplesArray = new int[samples.LongLength];
			for(long i = 0; i < samples.LongLength; i++)
			{
				samplesArray[i] = (int)Center(samples[i], Int32.MinValue, Int32.MaxValue);
			}
	        return samplesArray;
		}
		
		private long[] GetSamplesInt64(int sampleRate)
		{
			double[] samples = GetSamplesDouble(sampleRate);
			long[] samplesArray = new long[samples.LongLength];
			for(long i = 0; i < samples.LongLength; i++)
			{
				samplesArray[i] = (long)Center(samples[i], Int64.MinValue, Int64.MaxValue);
			}
	        return samplesArray;
		}
		
		private float[] GetSamplesSingle(int sampleRate)
		{
			double[] samples = GetSamplesDouble(sampleRate);
			float[] samplesArray = new float[samples.LongLength];
			for(long i = 0; i < samples.LongLength; i++)
			{
				samplesArray[i] = (float)(samples[i]);
			}
	        return samplesArray;
		}
		
		private static double Center(double val, double min, double max)
		{
			return Math.Round(min+(val+1)/2*(max-min), MidpointRounding.ToEven);
		}
		
		public T[] GetSamples<T>(int sampleRate=44100)
		{
			return (T[])(this.GetSamples(sampleRate, Type.GetTypeCode(typeof(T))));
		}
		
		public Array GetSamples(int sampleRate, TypeCode format)
		{
			switch(format)
			{
				case TypeCode.Byte:case TypeCode.SByte:
					return GetSamplesByte(sampleRate);
				case TypeCode.Int16:case TypeCode.UInt16:
					return GetSamplesInt16(sampleRate);
				case TypeCode.Int32:case TypeCode.UInt32:
					return GetSamplesInt32(sampleRate);
				case TypeCode.Int64:case TypeCode.UInt64:
					return GetSamplesInt64(sampleRate);
				case TypeCode.Single:
					return GetSamplesSingle(sampleRate);
				case TypeCode.Double:
					return GetSamplesDouble(sampleRate);
				default:
					throw new NotImplementedException();
			}
		}
	}
	
	public abstract class WaveBase : SampledBase, IWaveSound
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
		
		public bool NoClipping{
			get; set;
		}
		
		public override double Duration{
			get{
				return GetDuration();
			}
		}
		
		public WaveBase()
		{
			Volume = 1.0;
		}
		
		protected abstract double GetDuration();
		
		protected override double[] GetSamplesDouble(int sampleRate)
		{
			var sampleSize = (ulong)(sampleRate * GetDuration() / 1000);
			double[] samples = new double[sampleSize];
			WriteSamples(sampleRate, samples);
			if(NoClipping)
			{
				double max = 1.0;
				for(long i = 0; i < samples.LongLength; i++)
				{
					double sample = samples[i];
					if(Double.IsPositiveInfinity(sample)) sample = max;
					else if (Double.IsNegativeInfinity(sample)) sample = -max;
					else if(sample > max) max = sample;
					else if(sample < -max) max = -sample;
					else if(Double.IsNaN(sample)) sample = 0;
					samples[i] = sample;
				}
				if(max > 1.0)
				{
					for(long i = 0; i < samples.LongLength; i++)
					{
						samples[i] /= max;
					}
				}
			}else{
				for(long i = 0; i < samples.LongLength; i++)
				{
					double sample = samples[i];
					if(sample > 1) sample = 1;
					else if(sample < -1) sample = -1;
					else if(Double.IsNaN(sample)) sample = 0;
					samples[i] = sample;
				}
			}
			return samples;
		}
		
		public abstract WaveFunction ToFunction();
		protected abstract void WriteSamples(int sampleRate, double[] samples);
	}
	
	public class WaveSong : WaveBase
	{
		public List<Track> Waves{get; private set;}
		
		public WaveSong()
		{
			Waves = new List<Track>();
			Volume = 1;
		}
		
		protected override void WriteSamples(int sampleRate, double[] samples)
		{
			double rateDouble = sampleRate;
	        foreach(var wave in Waves)
	        {
	        	int sampleStart = (int)Math.Round(wave.Start/1000.0*sampleRate);
	        	int sampleLength = (int)Math.Floor(wave.Wave.Duration/1000.0*sampleRate);
	        	sampleLength = Math.Min(sampleLength, samples.Length-sampleStart);
	        	var waveFunc = wave.Wave.ToFunction();
	        	for(int i = 0; i < sampleLength; i++)
	        	{
	        		samples[sampleStart+i] += Volume * waveFunc[i / rateDouble] * wave.Wave.Volume / waveFunc.Amplitude;
	        	}
	        }
		}
		
		public void AddWave(int start, Wave wave)
		{
			Waves.Add(new Track(start, wave));
		}
		
		public new double Duration{
			get{
				return Waves.Max(w => w.Start+w.Wave.Duration);
			}
		}
		
		protected override double GetDuration()
		{
			return Duration;
		}
		
		public override WaveFunction ToFunction()
		{
			return new WaveFunction(
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
					return val*Volume;
				}, Volume*Waves.Max(w => w.Wave.ToFunction().Amplitude)
			);
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
		
		public static void WriteToFile(string file, ISampledSound samples, int sampleRate = 44100, TypeCode type=TypeCode.Int16)
		{
			WriteToFile(file, samples.GetSamples(sampleRate, type), sampleRate);
		}
		
		public static void WriteToFile(string file, Array samples, int sampleRate = 44100)
		{
			var writer = new WaveWriter();
			writer.SampleRate = sampleRate;
			using(var stream = new FileStream(file, FileMode.Create))
			{
				writer.WriteWave(stream, samples);
			}
		}
		
		public void WriteWave(Stream output, Array samples)
		{
			short format, bits;
			switch(Type.GetTypeCode(samples.GetType().GetElementType()))
			{
				case TypeCode.Byte:case TypeCode.SByte:
					format = 1;
					bits = 8;
					break;
				case TypeCode.Int16:case TypeCode.UInt16:
					format = 1;
					bits = 16;
					break;
				case TypeCode.Int32:case TypeCode.UInt32:
					format = 1;
					bits = 32;
					break;
				case TypeCode.Int64:case TypeCode.UInt64:
					format = 1;
					bits = 64;
					break;
				case TypeCode.Single:
					format = 3;
					bits = 32;
					break;
				case TypeCode.Double:
					format = 3;
					bits = 64;
					break;
				default:
					throw new NotImplementedException();
			}
			WriteWaveBase(output, format, bits, samples.Length);
			byte[] data = new byte[samples.Length*bits/8];
			Buffer.BlockCopy(samples, 0, data, 0, data.Length);
			output.Write(data, 0, data.Length);
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
		public static readonly ConstantWave Zero = new ConstantWave(0);
		public static readonly ConstantWave One = new ConstantWave(1);
		public static readonly PeriodicWave Sine = new PeriodicWave(x => Math.Sin(x*2*Math.PI));
		public static readonly PeriodicWave Square = Pulse(0.5);
		public static readonly PeriodicWave Triangle = new PeriodicWave(
			x => {
				if(x < 0.25) return 4*x;
				else if(x < 0.75) return 2-4*x;
				else return 4*x-4;
			}
		);
		public static readonly PeriodicWave Circle = new PeriodicWave(
			x => {
				if(x < 0.5) return Math.Sqrt(1.0-Math.Pow(4.0*x-1.0, 2));
				else return -Math.Sqrt(1.0-Math.Pow(4.0*x-3.0, 2));
			}
		);
		public static readonly PeriodicWave AbsSine = new PeriodicWave(
			x => Sine[x/2]
		);
		public static readonly PeriodicWave HalfSine = new PeriodicWave(
			x => {
				if(x < 0.5) return Sine[x];
				else return 0;
			}
		);
		public static readonly PeriodicWave HalfAbsSine = new PeriodicWave(
			x => {
				if(x < 0.5) return AbsSine[x];
				else return 0;
			}
		);
		public static readonly PeriodicWave AbsCircle = new PeriodicWave(
			x => Circle[x/2]
		);
		public static readonly PeriodicWave SineDouble = new PeriodicWave(
			x => {
				if(x <= 0.5)
				{
					return (Sine[x*2-0.25]+1)/2;
				}else{
					return -(Sine[x*2-0.25]+1)/2;
				}
			}
		);
		public static PeriodicWave SinePower(double exponent)
		{
			return new PeriodicWave(
				x => {
					if(x <= 0.5)
					{
						return Math.Abs(Math.Pow(Sine[x], exponent));
					}else{
						return -Math.Abs(Math.Pow(Sine[x-0.5], exponent));
					}
				}
			);
		}
		public static readonly PeriodicWave Sawtooth = new PeriodicWave(
			x => x*2-1
		);
		public static readonly PeriodicWave Clausen = new PeriodicWave(
			x => {
				x *= 2*Math.PI;
				var sin = Sine;
				double sum = 0;
				for(int i = 1; i <= 50; i++)
				{
					sum += Math.Sin(i*x)/(i*i);
				}
				return sum;
			}
		);
		public static PeriodicWave Pulse(double width)
		{
			return new PeriodicWave(
				x => {
					if(x <= width) return 1;
					else if(x > width) return -1;
					else return 0;
				}
			);
		}
		public static WaveFunction WhiteNoise = Noise(WaveFunction.One);
		public static WaveFunction Noise(WaveFunction coef)
		{
			Random rnd = new Random();
			return new WaveFunction(x => (rnd.NextDouble()*2-1)*coef[x], coef.Amplitude);
		}
		
		protected Func<double,double> Function{get; private set;}
		
		public virtual double this[double x]{
			get{
				double val = Function(x);
				if(val > Amplitude) return Amplitude;
				if(val < -Amplitude) return -Amplitude;
				return val;
			}
		}
		
		public virtual double Amplitude{
			get; private set;
		}
		
		public WaveFunction(Func<double,double> func, double amplitude)
		{
			Function = func;
			Amplitude = amplitude;
		}
		
		public static WaveFunction operator +(WaveFunction a, WaveFunction b)
		{
			return a.Combine(x=>a[x]+b[x], b, a.Amplitude+b.Amplitude);
		}
		public static WaveFunction operator -(WaveFunction a, WaveFunction b)
		{
			return a.Combine(x=>a[x]-b[x], b, a.Amplitude+b.Amplitude);
		}
		public static WaveFunction operator *(WaveFunction a, WaveFunction b)
		{
			return a.Combine(x=>a[x]*b[x], b, a.Amplitude*b.Amplitude);
		}
		public static WaveFunction operator /(WaveFunction a, double b)
		{
			return a.Combine(x=>a[x]/b, b, a.Amplitude/b);
		}
		public static WaveFunction operator ^(WaveFunction a, WaveFunction b)
		{
			return a.Combine(x=>Math.Pow(a[x], b[x]), b, Math.Pow(a.Amplitude, b.Amplitude));
		}
		public static WaveFunction operator -(WaveFunction wave)
		{
			return wave.Combine(x=>-wave[x], null, wave.Amplitude);
		}
		public static WaveFunction operator |(WaveFunction a, WaveFunction b)
		{
			return a.Join(b);
		}
		public static WaveFunction operator |(Func<double,double> func, WaveFunction wave)
		{
			return wave.On(func);
		}
		
		protected virtual WaveFunction Combine(Func<double, double> func, WaveFunction wave, double amplitude)
		{
			return new WaveFunction(
				func,
				amplitude
			);
		}
		protected virtual WaveFunction Join(WaveFunction wave)
		{
			return new WaveFunction(
				x => wave[this[x]],
				wave[this.Amplitude]
			);
		}
		protected virtual WaveFunction On(Func<double,double> func)
		{
			return new WaveFunction(
				x => this[func(x)],
				this.Amplitude
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
		
		public PeriodicWave(Func<double,double> func) : this(func, 1.0)
		{
			
		}
		
		public PeriodicWave(Func<double,double> func, double amplitude) : base(func, amplitude)
		{
			Rank = 1;
		}
		
		protected override WaveFunction Combine(Func<double, double> func, WaveFunction wave, double amplitude)
		{
			if(wave == null || wave is PeriodicWave)
			{
				return new PeriodicWave(
					func,
					amplitude
				);
			}else{
				return base.Combine(func, wave, amplitude);
			}
		}
		protected override WaveFunction Join(WaveFunction wave)
		{
			if(wave is PeriodicWave)
			{
				return new PeriodicWave(
					x => wave[this[x]],
					wave[this.Amplitude]
				);
			}else{
				return base.Join(wave);
			}
		}
		protected override WaveFunction On(Func<double,double> func)
		{
			return new PeriodicWave(
				x => this[func(x)],
				this.Amplitude
			);
		}
		
		public static PeriodicWave operator +(PeriodicWave a, PeriodicWave b)
		{
			return (PeriodicWave)((WaveFunction)a + b);
		}
		public static PeriodicWave operator -(PeriodicWave a, PeriodicWave b)
		{
			return (PeriodicWave)((WaveFunction)a - b);
		}
		public static PeriodicWave operator *(PeriodicWave a, PeriodicWave b)
		{
			return (PeriodicWave)((WaveFunction)a * b);
		}
		public static PeriodicWave operator /(PeriodicWave a, double b)
		{
			return (PeriodicWave)((WaveFunction)a / b);
		}
		public static PeriodicWave operator ^(PeriodicWave a, PeriodicWave b)
		{
			return (PeriodicWave)((WaveFunction)a ^ b);
		}
		public static PeriodicWave operator -(PeriodicWave wave)
		{
			return (PeriodicWave)(-(WaveFunction)wave);
		}
		
		public static PeriodicWave operator &(PeriodicWave a, PeriodicWave b)
		{
			var rsum = a.Rank + b.Rank;
			return new CombinedWave(
				x => {
					if(x < (double)a.Rank/rsum) return a[x/a.Rank*rsum];
					else return b[(x-a.Rank)/b.Rank*rsum];
				},
				Math.Max(a.Amplitude, b.Amplitude),
				rsum
			);
		}
		
		public static implicit operator PeriodicWave(int val)
		{
			return new ConstantWave(val);
		}
		public static implicit operator PeriodicWave(double val)
		{
			return new ConstantWave(val);
		}
		
		private int Rank{get; set;}
		
		private class CombinedWave : PeriodicWave
		{
			public CombinedWave(Func<double,double> func, double amplitude, int rank) : base(func, amplitude)
			{
				Rank = rank;
			}
			
			protected override WaveFunction Combine(Func<double, double> func, WaveFunction wave, double amplitude)
			{
				if(wave == null || wave is PeriodicWave)
				{
					return new CombinedWave(
						func,
						amplitude,
						Rank
					);
				}else{
					return base.Combine(func, wave, amplitude);
				}
			}
			protected override WaveFunction Join(WaveFunction wave)
			{
				if(wave is PeriodicWave)
				{
					return new CombinedWave(
						x => wave[this[x]],
						wave[this.Amplitude],
						Rank
					);
				}else{
					return base.Join(wave);
				}
			}
			protected override WaveFunction On(Func<double,double> func)
			{
				return new CombinedWave(
					x => this[func(x)],
					this.Amplitude,
					Rank
				);
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
		
		public override double Amplitude{
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