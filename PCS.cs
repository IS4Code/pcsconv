/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 19:58
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace speakerconv
{
	public abstract class Command
	{
		public abstract int Port{get;}
		public int Time{get; private set;}
		
		public Command(int time)
		{
			Time = time;
		}
	}
	
	public class EnableCommand : Command
	{
		public override int Port{get{return 0x61;}}
		public bool Enable{get; private set;}
		
		public EnableCommand(bool enable, int time) : base(time)
		{
			Enable = enable;
		}
	}
	
	public class FrequencyModeCommand : Command
	{
		public override int Port{get{return 0x43;}}
		public FrequencyMode Mode{get; private set;}
		
		public FrequencyModeCommand(FrequencyMode mode, int time) : base(time)
		{
			Mode = mode;
		}
		
		public FrequencyModeCommand(int mode, int time) : base(time)
		{
			Mode = (FrequencyMode)mode;
		}
	}
	
	public enum FrequencyMode : byte
	{
		FrequencyDivider = 2,
		Countdown = 3
	}
	
	public class FrequencyByteCommand : Command
	{
		public override int Port{get{return 0x42;}}
		public byte Value{get; private set;}
		
		public FrequencyByteCommand(byte value, int time) : base(time)
		{
			Value = value;
		}
	}
	
	public class FrequencyByte1Command : FrequencyByteCommand
	{
		public override int Port{get{return 0x42;}}
		public FrequencyByte1Command(byte value, int time) : base(value, time)
		{
			
		}
	}
	
	public class FrequencyByte2Command : FrequencyByteCommand
	{
		public override int Port{get{return 0x42;}}
		public FrequencyByte2Command(byte value, int time) : base(value, time)
		{
			
		}
	}
}
