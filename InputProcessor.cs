/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 7.12.2013
 * Time: 15:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace speakerconv
{
	public abstract class InputProcessor : IInputProcessor
	{
		public InputProcessor()
		{
			
		}
		
		public abstract IList<OutputFile> ProcessStream(Stream input, ConvertOptions options);
		
		public virtual IList<OutputFile> ProcessFile(string file, ConvertOptions options)
		{
			using(FileStream stream = new FileStream(file, FileMode.Open))
			{
				return ProcessStream(stream, options);
			}
		}
	}
	
	public interface IInputProcessor
	{
		IList<OutputFile> ProcessStream(Stream input, ConvertOptions options);
		IList<OutputFile> ProcessFile(string file, ConvertOptions options);
	}
}
