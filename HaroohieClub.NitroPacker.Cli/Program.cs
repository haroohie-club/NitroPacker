// See https://aka.ms/new-console-template for more information
using HaroohieClub.NitroPacker.Core;

string romPath = @"D:\dev\projects\nitro\mkds-decomp\masterrom\WUP-N-DACP\WUP-N-DACP.nds";
string outputPath = @"C:\Users\ermel\Desktop\Tests";

NdsProjectFile.Create("TestUnpack", romPath, outputPath);