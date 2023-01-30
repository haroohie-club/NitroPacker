using HaroohieClub.NitroPacker.Core;

string romPath = @"D:\dev\projects\nitro\mkds-decomp\masterrom\WUP-N-DACP\WUP-N-DACP.nds";
string unpackPath = @"C:\Users\ermel\Desktop\Tests";
string packPath = @"C:\Users\ermel\Desktop\Tests\Rom.nds";
string unpackedProjectName = "TestUnpack";
string unpackedProjectFilePath = Path.Combine(unpackPath, unpackedProjectName + ".xml");

NdsProjectFile.Create("TestUnpack", romPath, unpackPath);
NdsProjectFile.Pack(packPath, unpackedProjectFilePath);