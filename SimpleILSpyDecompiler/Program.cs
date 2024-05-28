using System.Text.Json;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using SimpleILSpyDecompiler;

PEFile peFile = new PEFile(args[0]);
UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(peFile.FileName, false,
  peFile.Metadata.DetectTargetFrameworkId(), peFile.DetectRuntimePack());

JsonSerializerOptions jsonSerializerOptions = new()
{
  WriteIndented = true,
  Converters = { new DecompilerSettingsJsonConverter() }
};

DecompilerSettings settings =
  JsonSerializer.Deserialize<DecompilerSettings>(File.ReadAllText("settings.json"), jsonSerializerOptions)!;

WholeProjectDecompiler decompiler = new WholeProjectDecompiler(settings, resolver, null, null);

if (!Directory.Exists(args[1]))
  Directory.CreateDirectory(args[1]);

decompiler.DecompileProject(peFile, args[1]);
