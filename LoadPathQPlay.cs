/* Date: 3.11.2016, Time: 22:12 */
using System;
using System.Collections.Generic;
using System.IO;

namespace speakerconv
{
	public class LoadPathQPlay : InputProcessor, IPathProcessor
	{
		public LoadPathQPlay()
		{
			
		}
		
		public override IList<OutputFile> ProcessFile(string file, ConvertOptions options)
		{
			List<RPCCommand> rpc = new List<RPCCommand>();
			rpc.AddRange(QPlay.GetCommands(file));
			return LoadPCS.ProcessRPC(rpc, options);
		}
		
		public override IList<OutputFile> ProcessStream(Stream input, ConvertOptions options)
		{
			throw new NotImplementedException();
		}
		
		public bool IsValidPath(string path)
		{
			return true;
		}
		
		public IEnumerable<string> GetInputs(string path)
		{
			yield return path;
		}
	}
}
