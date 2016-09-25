/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 19.10.2013
 * Time: 15:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using IllidanS4.Wave;

namespace speakerconv
{
	partial class Program
	{
		static readonly Dictionary<string, IInputProcessor> InputProcessors = new Dictionary<string, IInputProcessor>{
			{"pcs", new LoadPCS()},
			{"mdt", new LoadMDT()},
			{"bin", new LoadMDT()},
			{"txt", new LoadTXT()},
			{"dp", new LoadSaveDP()},
		};
		
		static readonly Dictionary<string, IOutputProcessor> OutputProcessors = new Dictionary<string, IOutputProcessor>{
			{"dro", new SaveDRO()},
			{"beep", new PlayBeep()},
			{"play", new PlayWAV()},
			{"wav", new SaveWAV()},
			{"droplay", new PlayDRO()},
			{"txt", new SaveTXT()},
			{"dp", new LoadSaveDP()},
		};
		
		public static void Main(string[] args)
		{
			/*WaveSong sng = new WaveSong{Volume = 0.5};
			var arr = new[]{
				WaveFunction.Sine, WaveFunction.AbsSine, WaveFunction.HalfSine, WaveFunction.HalfAbsSine, WaveFunction.Square, WaveFunction.Triangle, WaveFunction.AbsTriangle, WaveFunction.Circle, WaveFunction.AbsCircle
			};
			for(int i = 0; i < arr.Length; i++)
			{
				sng.AddWave(i*5000, new Wave(440, 4000){Type = new WaveFunction(x => Math.Pow(x, 1.04)) | arr[i]});
			}
			var writer = new WaveWriter();
			writer.WriteWave(new FileStream("wave.wav", FileMode.Create), sng.GetSamples());*/
			//var func = WaveFunction.AbsCircle;
			//var func = new WaveFunction(x => Math.Round(x*6)/6.0) | WaveFunction.Sine;
			//var func = new WaveFunction(x => Math.Max(Math.Min(Math.Tan(x*10)/10, 1.0), -1.0));
			//new WaveWriter().WriteWave(new FileStream("wave.wav", FileMode.Create), new WavePlayer().CreateWave(new Wave(440, 1000, func)));
			//new WavePlayer().PlayWave(new Wave(440, 1000, func));
			/*var song = new WaveSong();
			for(int i = 1; i <= 10; i++)
			{
				song.AddWave(0, new Wave(220*i, 1000));
			}
			song.Volume = 0.1;
			new WavePlayer().PlayWave(song);
			//new WaveWriter().WriteWave(new FileStream("wave.wav", FileMode.Create), new WavePlayer().CreateWave(song));
			Console.ReadKey(true);
			return;*/
			var options = ReadArguments(args);
			if(options == null) return;
			foreach(var option in options.GetInputs())
			{
				
				ProcessInput(option);
			}
		}
		
		private static ConvertOptions ReadArguments(string[] args)
		{
			var options = new ConvertOptions();
			
			var helpException = new Exception();
			string errorArg = null;
			try{
				if(args.Length == 0) throw new ArgumentException("No arguments passed.");
				var en = args.Cast<string>().GetEnumerator();
				while(en.MoveNext() && en.Current != null)
				{
					errorArg = en.Current;
					
					switch(en.Current.ToLower())
					{
						case "/w":case "-w":case "--waveform":
							en.MoveNext();
							options.Waveform = Int32.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "/o":case "-o":case "--opldata":
							en.MoveNext();
							string oplstr = en.Current;
							string[] ssplit = oplstr.Split('/', ',', ' ', '|', ':', ';', '-', '+');
							options.DRO_PrefixCommands.AddRange(ssplit.Select((s,i) => new{Index = i, Value = Byte.Parse(s, NumberStyles.HexNumber)}).GroupBy(x => x.Index / 2).Select(p => new DROCommand(p.ElementAt(0).Value, p.ElementAt(1).Value)));
							break;
						case "/t":case "-t":case "--trim":
							options.Trim = true;
							break;
						case "/s":case "-s":case "--split":
							options.Split = true;
							en.MoveNext();
							options.SplitDelay = Double.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "/f":case "-f":case "--filter":
							options.Filter = true;
							en.MoveNext();
							options.FilterDelay = Double.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "/c":case "-c":case "--crop":
							options.Crop = true;
							en.MoveNext();
							int sim = Int32.Parse(en.Current, CultureInfo.InvariantCulture);
							if(sim == -1) sim = Int32.MaxValue;
							options.CropSimilarity = sim;
							break;
						case "/l":case "-l":case "--length":
							options.TrimLength = true;
							en.MoveNext();
							options.NewLength = Int32.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "/d":case "-d":case "--delay":
							options.DRO_EndDelay = 200;
							break;
						case "/n":case "-n":case "--no-optimalization":
							options.DRO_Optimize = false;
							break;
						case "/m":case "-m":case "--multichannel":
							options.MultiChannel = true;
							break;
						case "/r":case "-r":case "--repeat":
							options.Repeat = true;
							en.MoveNext();
							options.RepeatCount = Int32.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "/in":case "-in":case "--input-type":
							en.MoveNext();
							options.InputType = en.Current;
							break;
						case "/out":case "-out":case "--output-type":
							en.MoveNext();
							options.OutputType = en.Current;
							break;
						case "/w:c":case "-w:c":case "--wave-clip":
							options.Wave_Clip = true;
							break;
						case "/w:v":case "-w:v":case "--wave-volume":
							en.MoveNext();
							options.Wave_Volume = Double.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "/w:r":case "-w:r":case "--wave-sample-rate":
							en.MoveNext();
							options.Wave_Frequency = Int32.Parse(en.Current, CultureInfo.InvariantCulture);
							break;
						case "?":case "/?":case "-?":case "/h":case "-h":case "--help":
							throw helpException;
						default:
							if(options.InputPath != null)
							{
								if(options.OutputPath != null)
								{
									throw new ArgumentException("Unexpected switch "+en.Current+".");
								}
								options.OutputPath = en.Current;
							}else{
								options.InputPath = en.Current;
							}
							break;
					}
				}
				if(options.InputPath == null) throw new ArgumentException("No path argument given.");
				if(options.OutputPath == null) options.OutputPath = Path.ChangeExtension(options.InputPath, options.Extension);
				if(!File.Exists(options.InputPath)) throw new ArgumentException("Invalid path.");
			}catch(Exception ex)
			{
				if(ex == helpException)
				{
					var ass = Assembly.GetExecutingAssembly().GetName();
					Console.WriteLine(ass.Name+" "+ass.Version.ToString(2)+" (c) 2013 - 2015 by IllidanS4@gmail.com");
					Console.WriteLine("Usage: pcsconv [options] [input path] [output path]");
					Console.WriteLine("Command-line arguments:");
					Console.WriteLine("  -? -h --help                      -- Shows this help.");
					Console.WriteLine("  -w --waveform [type]              -- Sets waveform type (default 2 for DRO and 4 for WAVE).");
					Console.WriteLine("  -o --opldata [register:value:...] -- Additional OPL commands.");
					Console.WriteLine("  -t --trim                         -- Trims delays from start and end.");
					Console.WriteLine("  -f --filter                       -- Removes unnecessary sound noises.");
					Console.WriteLine("  -s --split [mindelay]             -- Splits audio to multiple files.");
					Console.WriteLine("  -n --no-optimalization            -- Disables removing redundant commands.");
					Console.WriteLine("  -d --delay                        -- Adds 200ms delay to the end.");
					Console.WriteLine("  -l --length [length]              -- Crops the output to the specified length (in ms).");
					Console.WriteLine("  -m --multichannel                 -- Turns multichannel DRO on.");
					Console.WriteLine("  -r --repeat [count]               -- Repeats the song n-times.");
					Console.WriteLine("  -in  --input-type                 -- Specifies the input type (default is pcs).");
					Console.WriteLine("  -out --output-type                -- Specifies the output type (default is dro).");
					Console.WriteLine("Input types:");
					Console.WriteLine(" pcs - RPC text output from the modified DOSBox version.");
					Console.WriteLine(" mdt/bin - Binary output from MIDITONES.");
					Console.WriteLine(" txt - simple RPC commands.");
					Console.WriteLine(" dp - Doom PC Speaker.");
					Console.WriteLine("Output types:");
					Console.WriteLine(" dro - DOSBox Raw OPL.");
					Console.WriteLine(" droplay - DRO and plays it with 'dro_player' (needs to be available).");
					Console.WriteLine(" beep - Plays using console beeps.");
					Console.WriteLine(" wav - WAVE.");
					Console.WriteLine(" play - Plays using WAVE.");
					Console.WriteLine(" txt - simple RPC commands.");
					Console.WriteLine(" dp - Doom PC Speaker.");
					return null;
				}else{
					Error((errorArg != null && (errorArg.StartsWith("-")||errorArg.StartsWith("/"))?errorArg+": ":"")+ex.Message+" Try --help.");
				}
			}
			
			return options;
		}
		
		private static void ProcessInput(ConvertOptions options)
		{
			var files = InputProcessors[options.InputType].ProcessFile(options.InputPath, options);
			
			var output = files.Where(f => !options.Filter || f.Data.Sum(cmd => cmd.Type == RPCCommandType.Delay ? cmd.Data : 0) > options.FilterDelay*1000);
			OutputProcessors[options.OutputType].ProcessFiles(output, options);
		}
	}
	
	public class ConvertOptions : ICloneable
	{
		public string InputPath{get;set;}
		public string OutputPath{get;set;}
		public string InputType{get;set;}
		public string OutputType{get;set;}
		public string Extension{
			get{
				return Path.GetExtension(OutputPath);
			}
		}
		
		public bool Trim{get;set;}
		public bool Split{get;set;}
		public double SplitDelay{get;set;}
		public bool Crop{get;set;}
		public int CropSimilarity{get;set;}
		public bool Filter{get;set;}
		public double FilterDelay{get;set;}
		
		public bool TrimLength{get;set;}
		public int NewLength{get;set;}
		
		public bool MultiChannel{get;set;}
		
		public bool Repeat{get;set;}
		public int RepeatCount{get;set;}
		
		public bool DRO_Optimize{get;set;}
		public List<DROCommand> DRO_PrefixCommands{get;private set;}
		public int? Waveform{get;set;}
		public int DRO_EndDelay{get;set;}
		
		public double? Wave_Volume{get;set;}
		public bool? Wave_Clip{get;set;}
		public int? Wave_Frequency{get;set;}
		
		public ConvertOptions()
		{
			InputType = "pcs";
			OutputType = "dro";
			SplitDelay = Double.MaxValue;
			CropSimilarity = Int32.MaxValue;
			FilterDelay = 0.0;
			DRO_Optimize = true;
			DRO_PrefixCommands = new List<DROCommand>();
		}
		
		public IEnumerable<ConvertOptions> GetInputs()
		{
			foreach(string file in Directory.EnumerateFiles(Path.GetDirectoryName(Path.GetFullPath(InputPath)), Path.GetFileName(InputPath)))
			{
				ConvertOptions newOptions = (ConvertOptions)this.Clone();
				newOptions.InputPath = file;
				if(new Uri(Path.GetFullPath(InputPath)) != new Uri(Path.GetFullPath(file)))
				{
					newOptions.OutputPath = Path.Combine(Path.GetDirectoryName(OutputPath), Path.GetFileName(Path.ChangeExtension(file, Extension)));
				}
				yield return newOptions;
			}
		}
		
		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
	
	static class Extensions
	{
		public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> source, Func<T,bool> predicate)
		{
			List<T> list = new List<T>();
			foreach(T elem in source)
			{
				if(predicate(elem))
				{
					yield return list;
					list = new List<T>();
				}else{
					list.Add(elem);
				}
			}
			yield return list;
		}
		
		public static IEnumerable<T> ToIEnumerable<T>(this IEnumerable<T> source)
		{
			return source;
		}
		
		public static IList<T> ToIList<T>(this IList<T> source)
		{
			return source;
		}
		
		public static IEnumerable<T> Empty<T>(T example)
		{
			return new T[0];
		}
		
		/*public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection)
		{
			if(list is List<T>)
			{
				((List<T>)list).AddRange(collection);
			}else{
				foreach(T item in collection)
				{
					list.Add(item);
				}
			}
		}*/
	}
}