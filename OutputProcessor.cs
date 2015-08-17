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

namespace speakerconv
{
	public abstract class OutputProcessor
	{
		public OutputProcessor()
		{
			
		}
		
		public virtual void ProcessFiles(IEnumerable<OutputFile> files, ConvertOptions options)
		{
			foreach(var file in files) ProcessFile(file, options);
		}
		
		public abstract void ProcessFile(OutputFile file, ConvertOptions options);
	}
}
