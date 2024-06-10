using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text.Json;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using SimpleILSpyDecompiler;

RootCommand rootCommand = new("Decompile an assembly to a csproj project using ILSpy with custom settings supplied in JSON");
Command generateSettingsCommand = new("generate-settings", "Generate a JSON string containing all the default settings of ILSpy");
Command decompileCommand = new("decompile", "Decompile an assembly with a JSON settings file");

Argument<FileInfo> settingsArgument = new()
{
  Name = "settings",
  Description = "The JSON settings file. Any setting not specified will be their default value"
};
Argument<FileInfo> inputAssemblyArgument = new() {
  Name = "assembly",
  Description = "The assembly file to decompile to a csproj project"
};
Argument<DirectoryInfo> outputDirectoryArgument = new()
{
  Name = "output directory",
  Description = "The directory to output the csproj and project files. It will be created if it doesn't exist"
};

decompileCommand.AddAlias("d");
decompileCommand.AddArgument(inputAssemblyArgument);
decompileCommand.AddArgument(outputDirectoryArgument);
decompileCommand.AddArgument(settingsArgument);
decompileCommand.SetHandler(DecompileProject,
  inputAssemblyArgument, settingsArgument, outputDirectoryArgument);

generateSettingsCommand.AddAlias("g");
generateSettingsCommand.SetHandler(GetDefaultSettings);

rootCommand.AddCommand(decompileCommand);
rootCommand.AddCommand(generateSettingsCommand);

Parser parser = new CommandLineBuilder(rootCommand)
  .UseBetterDefaults(rootCommand)
  .Build();

await parser.InvokeAsync(args);
return;

void WriteIlSpyVersion()
{
  Assembly csharpDecompilerAssembly = Assembly.GetAssembly(typeof(ICSharpCode.Decompiler.CSharp.CSharpDecompiler))!;
  Console.WriteLine($"Using ILSpy version {csharpDecompilerAssembly.GetName().Version}");
}

void GetDefaultSettings()
{
  JsonSerializerOptions jsonSerializerOptions = new()
  {
    WriteIndented = true,
    Converters = { new DecompilerSettingsJsonConverter() }
  };

  DecompilerSettings settings = new();
  string json = JsonSerializer.Serialize(settings, jsonSerializerOptions);
  Console.WriteLine(json);
}

void DecompileProject(FileInfo assembly, FileInfo settingsFile, DirectoryInfo outputDirectory)
{
  WriteIlSpyVersion();
  PEFile peFile = new(assembly.FullName);
  UniversalAssemblyResolver resolver = new(peFile.FileName, false,
    peFile.Metadata.DetectTargetFrameworkId(), peFile.DetectRuntimePack());

  JsonSerializerOptions jsonSerializerOptions = new()
  {
    WriteIndented = true,
    Converters = { new DecompilerSettingsJsonConverter() }
  };

  DecompilerSettings settings =
    JsonSerializer.Deserialize<DecompilerSettings>(File.ReadAllText(settingsFile.FullName), jsonSerializerOptions)!;

  WholeProjectDecompiler decompiler = new(settings, resolver, null, null);

  if (!Directory.Exists(outputDirectory.FullName))
    Directory.CreateDirectory(outputDirectory.FullName);

  Console.WriteLine($"Decompiling {peFile.Name}...");
  decompiler.DecompileProject(peFile, outputDirectory.FullName);
  Console.WriteLine($"Done!");
}
