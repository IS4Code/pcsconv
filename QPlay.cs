/* Date: 3.11.2016, Time: 9:26 */
using System;
using System.Collections.Generic;

namespace speakerconv
{
	//Code ported from here: http://stackoverflow.com/a/11380262/1424244
	public class QPlay
	{
		static double Note2Freq(int Note) // Note=1 = C1 (32.7032 Hz), Note=84 = B7 (3951.07 Hz)
		{
		  double f = 0;
		  if (Note > 0)
		    f = 440 * Math.Exp(Math.Log(2) * (Note - 46) / 12);
		  return f;
		}
		
		static readonly int[] semitonesFromC = { 9, 11, 0, 2, 4, 5, 7 }; // A,B,C,D,E,F,G
		static int Name2SemitonesFromC(char c)
		{
		  if (c < 'A' && c > 'G') return -1;
		  return semitonesFromC[c - 'A'];
		}

		class Player
		{
			enum PlayerState
			{
				Parsing,
				Generating,
			}
			PlayerState State;
			
			int Tempo;
			int Duration;
			int Octave;
			enum PlayerMode
			{
				Normal,
				Legato,
				Staccato,
			}
			PlayerMode Mode;
			
			int Note;
			double NoteDuration;
			double NoteTime;
			uint SampleRate;
			
			public Player(uint sampleRate)
			{
				State = PlayerState.Parsing;
				Tempo = 120; // [32,255] quarter notes per minute
				Duration = 4; // [1,64]
				Octave = 4; // [0,6]
				Mode = PlayerMode.Normal;
				Note = 0;
				SampleRate = sampleRate;
			}
			
			public bool NextNote(string ppMusicString, ref int pos)
			{
				int number;
				int note = 0;
				int duration = 0;
				int dotCnt = 0;
				
				while (State == PlayerState.Parsing)
				{
					char c = ppMusicString[pos];
				
					if (c == '\0') return false;
					
					pos++;
					
					if (Char.IsWhiteSpace(c)) continue;
					
					c = Char.ToUpperInvariant(c);
					
					switch (c)
					{
						case 'O':
							c = ppMusicString[pos];
							if (c < '0' || c > '6') return false;
							Octave = c - '0';
							pos++;
							break;
						
						case '<':
							if (Octave > 0) Octave--;
							break;
						
						case '>':
							if (Octave < 6) Octave++;
							break;
						
						case 'M':
							c = Char.ToUpperInvariant(ppMusicString[pos]);
							switch (c)
							{
								case 'L':
									Mode = PlayerMode.Legato;
									break;
								case 'N':
									Mode = PlayerMode.Normal;
									break;
								case 'S':
									Mode = PlayerMode.Staccato;
									break;
								case 'B':
								case 'F':
									// skip MB and MF
									break;
								default:
									return false;
							}
						pos++;
						break; // ML/MN/MS, MB/MF
					
					case 'L':
					case 'T':
						number = 0;
						for(;;)
						{
							char c2 = ppMusicString[pos];
							if(Char.IsDigit(c2))
							{
								number = number * 10 + c2 - '0';
								pos++;
							}
							else break;
						}
						switch(c)
						{
							case 'L':
								if (number < 1 || number > 64) return false;
								Duration = number;
								break;
							case 'T':
								if (number < 32 || number > 255) return false;
								Tempo = number;
								break;
						}
						break; // Ln/Tn
					
					case 'A': case 'B': case 'C': case 'D':
					case 'E': case 'F': case 'G':
					case 'N':
					case 'P':
						switch (c)
						{
							case 'A': case 'B': case 'C': case 'D':
							case 'E': case 'F': case 'G':
								note = 1 + Octave * 12 + Name2SemitonesFromC(c);
								break; // A...G
							case 'P':
								note = 0;
								break; // P
							case 'N':
								number = 0;
								for(;;)
								{
									char c2 = ppMusicString[pos];
									if(Char.IsDigit(c2))
									{
										number = number * 10 + c2 - '0';
										pos++;
									}
									else break;
								}
								if (number < 0 || number > 84) return false;
								note = number;
								break; // N
						} // got note #
					
						if(c >= 'A' && c <= 'G')
						{
							char c2 = ppMusicString[pos];
							if(c2 == '+' || c2 == '#')
							{
								if(note < 84) note++;
								pos++;
							}
							else if (c2 == '-')
							{
								if (note > 1) note--;
								pos++;
							}
						} // applied sharps and flats
						
						duration = Duration;
						
						if(c != 'N')
						{
							number = 0;
							for(;;)
							{
								char c2 = ppMusicString[pos];
								if(Char.IsDigit(c2))
								{
									number = number * 10 + c2 - '0';
									pos++;
								}
								else break;
							}
							if (number < 0 || number > 64) return false;
							if (number > 0) duration = number;
						} // got note duration
						
						while (ppMusicString[pos] == '.')
						{
							dotCnt++;
							pos++;
						} // got dots
						
						Note = note;
						NoteDuration = 1.0 / duration;
						while(dotCnt-- != 0)
						{
							duration *= 2;
							NoteDuration += 1.0 / duration;
						}
						NoteDuration *= 60 * 4.0 / Tempo; // in seconds now
						NoteTime = 0;
						
						State = PlayerState.Generating;
						break; // A...G/N/P
					
					default:
						return false;
					} // switch (c)
				}

				return true;
			}
			
			public IEnumerable<RPCCommand> GetCommands(string ppMusicString)
			{
				int pos = 0;
				while(NextNote(ppMusicString, ref pos))
				{
					double freq = Note2Freq(Note) * 2;
					NoteTime += NoteDuration;
					State = PlayerState.Parsing;
					double silence = 0;
					if(Mode == PlayerMode.Normal)
					{
						silence = NoteDuration / 8;
					}else if(Mode == PlayerMode.Staccato)
					{
						silence = NoteDuration / 4;
					}
					if(freq > 0)
					{
						yield return RPCCommand.SetCountdown(LoadMDT.FrequencyToCountdown(freq));
					}else{
						yield return RPCCommand.ClearCountdown();
					}
					yield return RPCCommand.Delay((int)Math.Round((NoteDuration-silence)*1000));
					if(silence > 0)
					{
						yield return RPCCommand.ClearCountdown();
						yield return RPCCommand.Delay((int)Math.Round(silence*1000));
					}
				}
			}
			
			public bool GetSample(string ppMusicString, ref int pos, out short pSample)
			{
				double sample;
				double freq;
				
				pSample = 0;
				if(!NextNote(ppMusicString, ref pos)) return false;
				
				// pPlayer->State == StateGenerating
				// Calculate the next sample for the current note
				
				sample = 0;
				
				// QuickBasic Play() frequencies appear to be 1 octave higher than
				// on the piano.
				freq = Note2Freq(Note) * 2;
				
				if (freq > 0)
				{
					double f = freq;
					
					while (f < SampleRate / 2 && f < 8000) // Cap max frequency at 8 KHz
					{
						sample += Math.Exp(-0.125 * f / freq) * Math.Sin(2 * Math.PI * f * NoteTime);
						f += 2 * freq; // Use only odd harmonics
					}
					
					sample *= 15000;
					sample *= Math.Exp(-NoteTime / 0.5); // Slow decay
				}
				
				if((Mode == PlayerMode.Normal && NoteTime >= NoteDuration * 7 / 8) ||
					(Mode == PlayerMode.Staccato && NoteTime >= NoteDuration * 3 / 4))
					sample = 0;
				
				if(sample > 32767) sample = 32767;
				if(sample < -32767) sample = -32767;
				
				pSample = (short)sample;
				
				NoteTime += 1.0 / SampleRate;
				
				if (NoteTime >= NoteDuration)
					State = PlayerState.Parsing;
				
				return true;
			}
		}
		
		public static IEnumerable<short> GetSamples(string musicString)
		{
			var player = new Player(44100);
			short sample;
			int pos = 0;
			musicString = musicString+"\0";
			while(player.GetSample(musicString, ref pos, out sample))
				yield return sample;
		}
		
		public static IEnumerable<RPCCommand> GetCommands(string musicString)
		{
			var player = new Player(44100);
			musicString = musicString+"\0";
			return player.GetCommands(musicString);
		}
		
		/*
int PlayerGetSample(tPlayer* pPlayer, const char** ppMusicString, short* pSample)
{
}

int PlayToFile(const char* pFileInName, const char* pFileOutName, unsigned SampleRate)
{
  int err = EXIT_FAILURE;
  FILE *fileIn = NULL, *fileOut = NULL;
  tPlayer player;
  short sample;
  char* pMusicString = NULL;
  const char* p;
  size_t sz = 1, len = 0;
  char c;
  unsigned char uc;
  unsigned long sampleCnt = 0, us;

  if ((fileIn = fopen(pFileInName, "rb")) == NULL)
  {
    fprintf(stderr, "can't open file \"%s\"\n", pFileInName);
    goto End;
  }

  if ((fileOut = fopen(pFileOutName, "wb")) == NULL)
  {
    fprintf(stderr, "can't create file \"%s\"\n", pFileOutName);
    goto End;
  }

  if ((pMusicString = malloc(sz)) == NULL)
  {
NoMemory:
    fprintf(stderr, "can't allocate memory\n");
    goto End;
  }

  // Load the input file into pMusicString[]

  while (fread(&c, 1, 1, fileIn))
  {
    pMusicString[len++] = c;

    if (len == sz)
    {
      char* p;

      sz *= 2;
      if (sz < len)
        goto NoMemory;

      p = realloc(pMusicString, sz);
      if (p == NULL)
        goto NoMemory;

      pMusicString = p;
    }
  }

  pMusicString[len] = '\0'; // Make pMusicString[] an ASCIIZ string

  // First, a dry run to simply count samples (needed for the WAV header)

  PlayerInit(&player, SampleRate);
  p = pMusicString;
  while (PlayerGetSample(&player, &p, &sample))
    sampleCnt++;

  if (p != pMusicString + len)
  {
    fprintf(stderr,
            "Parsing error near byte %u: \"%c%c%c\"\n",
            (unsigned)(p - pMusicString),
            (p > pMusicString) ? p[-1] : ' ',
            p[0],
            (p - pMusicString + 1 < len) ? p[1] : ' ');
    goto End;
  }

  // Write the output file

  // ChunkID
  fwrite("RIFF", 1, 4, fileOut);

  // ChunkSize
  us = 36 + 2 * sampleCnt;
  uc = us % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 / 256 / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);

  // Format + Subchunk1ID
  fwrite("WAVEfmt ", 1, 8, fileOut);

  // Subchunk1Size
  uc = 16;
  fwrite(&uc, 1, 1, fileOut);
  uc = 0;
  fwrite(&uc, 1, 1, fileOut);
  fwrite(&uc, 1, 1, fileOut);
  fwrite(&uc, 1, 1, fileOut);

  // AudioFormat
  uc = 1;
  fwrite(&uc, 1, 1, fileOut);
  uc = 0;
  fwrite(&uc, 1, 1, fileOut);

  // NumChannels
  uc = 1;
  fwrite(&uc, 1, 1, fileOut);
  uc = 0;
  fwrite(&uc, 1, 1, fileOut);

  // SampleRate
  uc = SampleRate % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = SampleRate / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = 0;
  fwrite(&uc, 1, 1, fileOut);
  fwrite(&uc, 1, 1, fileOut);

  // ByteRate
  us = (unsigned long)SampleRate * 2;
  uc = us % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 / 256 / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);

  // BlockAlign
  uc = 2;
  fwrite(&uc, 1, 1, fileOut);
  uc = 0;
  fwrite(&uc, 1, 1, fileOut);

  // BitsPerSample
  uc = 16;
  fwrite(&uc, 1, 1, fileOut);
  uc = 0;
  fwrite(&uc, 1, 1, fileOut);

  // Subchunk2ID
  fwrite("data", 1, 4, fileOut);

  // Subchunk2Size
  us = sampleCnt * 2;
  uc = us % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);
  uc = us / 256 / 256 / 256 % 256;
  fwrite(&uc, 1, 1, fileOut);

  // Data
  PlayerInit(&player, SampleRate);
  p = pMusicString;
  while (PlayerGetSample(&player, &p, &sample))
  {
    uc = (unsigned)sample % 256;
    fwrite(&uc, 1, 1, fileOut);
    uc = (unsigned)sample / 256 % 256;
    fwrite(&uc, 1, 1, fileOut);
  }

  err = EXIT_SUCCESS;

End:

  if (pMusicString != NULL) free(pMusicString);
  if (fileOut != NULL) fclose(fileOut);
  if (fileIn != NULL) fclose(fileIn);

  return err;
}

int main(int argc, char** argv)
{
  if (argc == 3)
//    return PlayToFile(argv[1], argv[2], 44100); // Use this for 44100 sample rate
    return PlayToFile(argv[1], argv[2], 16000);

  printf("Usage:\n  play2wav <Input-QBASIC-Play-String-file> <Output-Wav-file>\n");
  return EXIT_FAILURE;
}*/
	}
}
