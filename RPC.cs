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
	public sealed class RPCCommand
	{
		public RPCCommandType Type{get; private set;}
		public int Channel{get; private set;}
		public int Data{get; private set;}
		
		public RPCCommand(RPCCommandType type, int data)
		{
			Type = type;
			Data = data;
		}
		
		public RPCCommand(RPCCommandType type, int channel, int data)
		{
			Type = type;
			Channel = channel;
			Data = data;
		}
		
		public static RPCCommand ClearCountdown()
		{
			return new RPCCommand(RPCCommandType.ClearCountdown, 0);
		}
		
		public static RPCCommand ClearCountdown(int channel)
		{
			return new RPCCommand(RPCCommandType.ClearCountdown, channel, 0);
		}
		
		public static RPCCommand SetCountdown(int countdown)
		{
			return new RPCCommand(RPCCommandType.SetCountdown, countdown);
		}
		
		public static RPCCommand SetCountdown(int channel, int countdown)
		{
			return new RPCCommand(RPCCommandType.SetCountdown, channel, countdown);
		}
		
		public static RPCCommand Delay(int delay)
		{
			return new RPCCommand(RPCCommandType.Delay, delay);
		}
		
		public override string ToString()
		{
			return String.Format("{0}: {1}", Type, Data);
		}
		
		public int DelayValue{
			get{
				return Type == RPCCommandType.Delay ? Data : 0;
			}
		}
	}
	
	public enum RPCCommandType
	{
		None,
		Delay,
		SetCountdown,
		ClearCountdown
	}
}
