/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 19:59
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace speakerconv
{
	partial class Program
	{
		public static void Notice(string message, params object[] formatArgs)
		{
			Console.WriteLine(message, formatArgs);
		}
		
		public static void Warning(string message, params object[] formatArgs)
		{
			Console.WriteLine(message, formatArgs);
		}
		
		public static void Error(string message, params object[] formatArgs)
		{
			Console.WriteLine(message, formatArgs);
			Environment.Exit(0);
		}
	}
}
