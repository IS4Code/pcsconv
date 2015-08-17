using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/*
 * Created by SharpDevelop.
 * User: Illidan
 * Date: 2.11.2013
 * Time: 19:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
namespace speakerconv
{
	public sealed class DROCommand
	{
		public DRORegister Register{get; private set;}
		public int Value{get; private set;}
		
		public int Length{
			get{
				return Register.GetDataSize()+1;
			}
		}
		public int DataLength{
			get{
				return Register.GetDataSize();
			}
		}
		public byte[] Data{
			get{
				int size = Register.GetDataSize();
				byte[] data = new byte[size];
				Array.Copy(BitConverter.GetBytes(Value), data, size);
				return data;
			}
		}
		
		public byte OPLRegister{
			get{
				if(Register == DRORegister.Escape)
				{
					return Data[0];
				}else if((byte)Register <= 4)
				{
					return 255;
				}else{
					return (byte)Register;
				}
			}
		}
		
		public byte OPLValue{
			get{
				if(Register == DRORegister.Escape)
				{
					return Data[1];
				}else if((byte)Register <= 4)
				{
					return 255;
				}else{
					return (byte)Value;
				}
			}
		}
		
		public bool IsDelay{
			get{
				return Register == DRORegister.Delay || Register == DRORegister.DelayShort;
			}
		}
		
		public bool IsOPL{
			get{
				return (byte)Register > 4 || Register == DRORegister.Escape;
			}
		}
		
		public int DelayValue{
			get{
				return IsDelay?Value+1:0;
			}
		}
		
		public DROCommand(DRORegister register) : this(register, 0)
		{
			
		}
		
		public DROCommand(DRORegister register, int value)
		{
			Register = register;
			Value = value;
			
			/*byte reg = (byte)register;
			if((reg > 0xA8 && reg <= 0xAF) || (reg > 0xB8 && reg <= 0xBF) || (reg > 0xC8 && reg <= 0xCF) ||
			   (reg > 0x35 && reg <= 0x3F) || (reg > 0x55 && reg <= 0x5F) || (reg > 0x75 && reg <= 0x7F) ||
			   (reg > 0x95 && reg <= 0x9F) || (reg > 0xF5 && reg <= 0xFF))
			{
				throw new ArgumentOutOfRangeException("register");
			}*/
		}
		
		public DROCommand(DRORegister register, uint value) : this(register, unchecked((int)value))
		{
			
		}
		
		public DROCommand(byte register, int value) : this(register <= 0x04 ? DRORegister.Escape : (DRORegister)register, register <= 0x04 ? register | (value << 8) : value)
		{
			
		}
		
		public DROCommand(int register, int value) : this((byte)register, value)
		{
			
		}
		
		public override string ToString()
		{
			return String.Format("{0}: {1}", IsOPL?OPLRegister.ToString():Register.ToString(), Value);
		}
		
		public static IEnumerable<DROCommand> Delay(int time)
		{
			if(time == 0) yield break;
			uint timeraw = unchecked((uint)time-1);
			if(timeraw >= 256)
			{
				yield return new DROCommand(DRORegister.Delay, timeraw);
			}else{
				yield return new DROCommand(DRORegister.DelayShort, timeraw);
			}
		}
	}
	
	public enum DRORegister : byte
	{
		[DataSize(1)]
		DelayShort = 0,
		[DataSize(2)]
		Delay = 1,
		[DataSize(0)]
		ChipLow = 2,
		[DataSize(0)]
		ChipHigh = 3,
		[DataSize(2)]
		Escape = 4
	}
	
	public class DataSizeAttribute : Attribute
	{
		public int Size{get; private set;}
		
		public DataSizeAttribute(int bytesize)
		{
			Size = bytesize;
		}
	}
	
	public static class DRORegister_Extension
	{
		static readonly Dictionary<DRORegister,int> sizes;
		
		static DRORegister_Extension()
		{
			sizes = new Dictionary<DRORegister,int>();
			foreach(FieldInfo fi in typeof(DRORegister).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				DRORegister dr = (DRORegister)fi.GetValue(null);
				DataSizeAttribute attr = fi.GetCustomAttributes(typeof(DataSizeAttribute), true).First() as DataSizeAttribute;
				if(attr != null)
				{
					sizes[dr] = attr.Size;
				}
			}
		}
		
		public static int GetDataSize(this DRORegister register)
		{
			int size;
			if(!sizes.TryGetValue(register, out size)) return 1;
			return size;
		}
	}
}
