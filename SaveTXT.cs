/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 19:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace speakerconv
{
	public class SaveTXT : OutputProcessor
	{
		public override void ProcessFile(OutputFile file, ConvertOptions options)
		{
			using(StreamWriter writer = new StreamWriter(new FileStream(file.Path, FileMode.Create)))
			{
				foreach(var cmd in file.Data)
				{
					writer.WriteLine(cmd.ToString());
				}
			}
		}
	}
}
