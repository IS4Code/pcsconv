/* Date: 3.11.2016, Time: 22:17 */
using System;
using System.Collections.Generic;

namespace speakerconv
{
	public abstract class PathProcessor : IPathProcessor
	{
		public PathProcessor()
		{
			
		}
		
		public abstract bool IsValidPath(string path);
		
		public virtual IEnumerable<string> GetInputs(string path)
		{
			if(IsValidPath(path)) yield return path;
		}
	}
	
	public interface IPathProcessor
	{
		bool IsValidPath(string path);
		IEnumerable<string> GetInputs(string path);
	}
}
