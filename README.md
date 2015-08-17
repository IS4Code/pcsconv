pcsconv is a tool that can convert songs in simple (RPC) format, with only frequency and delays. Used to process output from the modified DOSBox.

Usage: pcsconv [options] [input path] [output path]
Command-line arguments:
  -? -h --help                      -- Shows this help.
  -w --waveform [type]              -- Sets waveform type (default 2).
  -o --opldata [register:value:...] -- Additional OPL commands.
  -t --trim                         -- Trims delays from start and end.
  -f --filter                       -- Removes unnecessary sound noises.
  -s --split [mindelay]             -- Splits audio to multiple files.
  -n --no-optimalization            -- Disables removing redundant commands.
  -d --delay                        -- Adds 200ms delay to the end.
  -l --length [length]              -- Crops the output to the specified length (in ms).
  -m --multichannel                 -- Turns multichannel DRO on.
  -r --repeat [count]               -- Repeats the song n-times.
  -in  --input-type                 -- Specifies the input type (default is pcs).
  -out --output-type                -- Specifies the output type (default is dro).
Input types:
 pcs - RPC text output from the modified DOSBox version.
 mdt/bin - Binary output from MIDITONES.
 txt - simple RPC commands.
Output types:
 dro - DOSBox Raw OPL.
 droplay - DRO and plays it with 'dro_player' (needs to be available).
 play - Plays using console beeps.
 txt - simple RPC commands.