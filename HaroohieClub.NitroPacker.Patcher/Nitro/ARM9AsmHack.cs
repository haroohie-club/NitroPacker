// This code is heavily based on code Gericom wrote for ErmiiBuild

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace HaroohieClub.NitroPacker.Patcher.Nitro
{
    public class ARM9AsmHack
	{
		public static bool Insert(string path, ARM9 arm9, uint arenaLoOffset, bool useDocker, DataReceivedEventHandler outputDataReceived = null, DataReceivedEventHandler errorDataReceived = null)
		{
			uint arenaLo = arm9.ReadU32LE(arenaLoOffset);
            if (!Compile(path, arenaLo, outputDataReceived, errorDataReceived, useDocker))
            {
                return false;
            }
			if (!File.Exists(Path.Combine(path, "newcode.bin")))
			{
				return false;
			}
			byte[] newCode = File.ReadAllBytes(Path.Combine(path, "newcode.bin"));

			StreamReader r = new(Path.Combine(path, "newcode.sym"));
			string[] newSymLines = File.ReadAllLines(Path.Combine(path, "newcode.sym"));
            List<string> newSymbolsFile = new();
            foreach (string line in newSymLines)
            {
                Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w\s+.text\s+\d{8} (?<name>.+)");
                if (match.Success)
                {
                    newSymbolsFile.Add($"{match.Groups["name"].Value} = 0x{match.Groups["address"].Value.ToUpper()};");
                }
            }
            File.WriteAllLines(Path.Combine(path, "newcode.x"), newSymbolsFile);

            // Each repl should be compiled separately since they all have their own entry points
            // That's why each one lives in its own separate directory
            List<string> replFiles = new();
            if (Directory.Exists(Path.Combine(path, "replSource")))
            {
                foreach (string subdir in Directory.GetDirectories(Path.Combine(path, "replSource")))
                {
                    replFiles.Add($"repl_{Path.GetFileNameWithoutExtension(subdir)}");
                    if (!CompileReplace(Path.GetRelativePath(path, subdir), path, outputDataReceived, errorDataReceived, useDocker))
                    {
                        return false;
                    }
                }
            }

            foreach (string replFile in replFiles)
            {
                if (!File.Exists(Path.Combine(path, $"{replFile}.bin")))
                {
                    return false;
                }
            }

            string currentLine;
			while ((currentLine = r.ReadLine()) != null)
			{
				string[] lines = currentLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (lines.Length == 4)
				{
					if (lines[3].Length < 7) continue;
					switch (lines[3].Remove(6))
					{
						case "ahook_":
							{
								string replaceOffsetString = lines[3].Replace("ahook_", "");
								uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
								uint replace = 0xEB000000; //BL Instruction
								uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
								uint relativeDestinationOffset = (destinationOffset / 4) - (replaceOffset / 4) - 2;
								relativeDestinationOffset &= 0x00FFFFFF;
								replace |= relativeDestinationOffset;
                                if (!arm9.WriteU32LE(replaceOffset, replace))
                                {
                                    throw new Exception(
                                        $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset."
                                        );
                                }
								break;
							}
						case "ansub_":
							{
								string replaceOffsetString = lines[3].Replace("ansub_", "");
								uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
								uint replace = 0xEA000000;//B Instruction
								uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
								uint relativeDestinationOffset = (destinationOffset / 4) - (replaceOffset / 4) - 2;
								relativeDestinationOffset &= 0x00FFFFFF;
								replace |= relativeDestinationOffset;
                                if (!arm9.WriteU32LE(replaceOffset, replace))
                                {
                                    throw new Exception(
                                        $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset."
                                        );
                                }
								break;
							}
						case "thook_":
							{
								string replaceOffsetString = lines[3].Replace("trepl_", "");
								uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
								ushort replace1 = 0xF000;//BLX Instruction (Part 1)
								ushort replace2 = 0xE800;//BLX Instruction (Part 2)
								uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
								uint relativeDestinationOffset = destinationOffset - replaceOffset - 2;
								relativeDestinationOffset >>= 1;
								relativeDestinationOffset &= 0x003FFFFF;
								replace1 |= (ushort)((relativeDestinationOffset >> 11) & 0x7FF);
								replace2 |= (ushort)((relativeDestinationOffset >> 0) & 0x7FE);
								if (!arm9.WriteU16LE(replaceOffset, replace1)) 
                                {
                                    throw new Exception(
                                        $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset.\r\nIf your code is inside an overlay, this is an action replay code to let your asm hack still work:\r\n1 {replaceOffset:X7} 0000{replace1:X4}\r\n1{replaceOffset + 2:X7} 0000{replace2:X4})"
                                        );
                                }
								else arm9.WriteU16LE(replaceOffset + 2, replace2);
								break;
							}
					}
				}
			}
			r.Close();
			arm9.WriteU32LE(arenaLoOffset, arenaLo + (uint)newCode.Length);
			arm9.AddAutoLoadEntry(arenaLo, newCode);

            // Perform the replacements for each of the replacement hacks we assembled
            foreach (string replFile in replFiles)
            {
                byte[] replCode = File.ReadAllBytes(Path.Combine(path, $"{replFile}.bin"));
                uint replaceAddress = uint.Parse(replFile.Split('_')[1], NumberStyles.HexNumber);
                arm9.WriteBytes(replaceAddress, replCode);
            }

            File.Delete(Path.Combine(path, "newcode.bin"));
			File.Delete(Path.Combine(path, "newcode.elf"));
			File.Delete(Path.Combine(path, "newcode.sym"));
            foreach (string overlayDirectory in Directory.GetDirectories(Path.Combine(path, "overlays")))
            {
                File.Copy(Path.Combine(path, "newcode.x"), Path.Combine(overlayDirectory, "arm9_newcode.x"), overwrite: true);
            }
            File.Delete(Path.Combine(path, "newcode.x"));
            foreach (string replFile in replFiles)
            {
                File.Delete(Path.Combine(path, $"{replFile}.bin"));
                File.Delete(Path.Combine(path, $"{replFile}.elf"));
                File.Delete(Path.Combine(path, $"{replFile}.sym"));
            }
            Directory.Delete(Path.Combine(path, "build"), true);
			return true;
		}

		private static bool Compile(string path, uint arenaLo, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, bool useDocker)
		{
            ProcessStartInfo psi;
            if (useDocker)
            {
                psi = new()
                {
                    FileName = "docker",
                    Arguments = $"run -v {Path.GetFullPath(path)}:/src -w /src devkitpro/devkitarm make TARGET=newcode SOURCES=source BUILD=build CODEADDR=0x{arenaLo:X8}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
            }
            else
            {
                psi = new()
                {
                    FileName = "make",
                    Arguments = $"TARGET=newcode SOURCES=source BUILD=build CODEADDR=0x{arenaLo:X8}",
                    WorkingDirectory = path,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
            }
            Process p = new() { StartInfo = psi };
            static void func(object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine(e.Data);
            }
            p.OutputDataReceived += outputDataReceived is not null ? outputDataReceived : func;
            p.ErrorDataReceived += errorDataReceived is not null ? errorDataReceived : func;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        private static bool CompileReplace(string subdir, string path, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, bool useDocker)
        {
            uint address = uint.Parse(Path.GetFileNameWithoutExtension(subdir), NumberStyles.HexNumber);
            ProcessStartInfo psi;
            if (useDocker)
            {
                psi = new()
                {
                    FileName = "docker",
                    Arguments = $"run -v {Path.GetFullPath(path)}:/src -w /src devkitpro/devkitarm make TARGET=repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir} BUILD=build NEWSYM=newcode.x BUILD=build CODEADDR=0x{address:X7}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
            }
            else
            {
                psi = new()
                {
                    FileName = "make",
                    Arguments = $"TARGET=repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir} BUILD=build NEWSYM=newcode.x BUILD=build CODEADDR=0x{address:X7}",
                    WorkingDirectory = path,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
            }
            Process p = new() { StartInfo = psi };
            static void func(object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine(e.Data);
            }
            p.OutputDataReceived += outputDataReceived is not null ? outputDataReceived : func;
            p.ErrorDataReceived += errorDataReceived is not null ? errorDataReceived : func;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }
    }
}
