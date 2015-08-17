/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 17:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace speakerconv
{
	/// <summary>
	/// Description of OutputFile.
	/// </summary>
	public class OutputFile
	{
		public string Path{get;set;}
		public List<RPCCommand> Data{get;set;}
		
		public OutputFile(string path, List<RPCCommand> data)
		{
			Path = path;
			Data = data;
		}
	}
}
