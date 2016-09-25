/* Date: 21.9.2015, Time: 14:48 */
using System;
using System.IO;
using System.Linq;
using System.Media;
using IllidanS4.Wave;

namespace speakerconv
{
	public class PlayWAV : OutputProcessor
	{
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			Console.WriteLine(file.Path);
			Console.WriteLine(TimeSpan.FromMilliseconds(file.Data.Sum(cmd => cmd.DelayValue)));
			Console.WriteLine("Creating WAV...");
			var song = SaveWAV.CreateSong(file.Data, SaveWAV.GetWaveform(options), options.Wave_Volume??1.0, options.Wave_Clip??false, options.Wave_Frequency??44100);
			Console.WriteLine("Playing...");
	        using(var buffer = new MemoryStream())
	        {
	        	SaveWAV.Writer.WriteWave(buffer, song);
			    buffer.Position = 0;
			    var player = new SoundPlayer(buffer);
			    player.PlaySync();
	        }
		}
	}
}
