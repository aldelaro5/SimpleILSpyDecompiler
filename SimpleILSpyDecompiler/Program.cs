using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using SimpleILSpyDecompiler;

RootCommand rootCommand =
  new("Decompile an assembly to a csproj project using ILSpy with custom settings supplied in JSON");
Command generateSettingsCommand =
  new("generate-settings", "Generate a JSON string containing all the default settings of ILSpy");
Command decompileCommand = new("decompile", "Decompile an assembly with a JSON settings file");
Command disassembleCommand = new("disassemble", "Disassemble an assembly");

Argument<FileInfo> settingsArgument = new()
{
  Name = "settings",
  Description = "The JSON settings file. Any setting not specified will be their default value"
};
Argument<FileInfo> inputAssemblyArgument = new()
{
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

disassembleCommand.AddAlias("dis");
disassembleCommand.AddArgument(inputAssemblyArgument);
disassembleCommand.AddArgument(outputDirectoryArgument);
disassembleCommand.SetHandler(DisassembleAssembly, inputAssemblyArgument, outputDirectoryArgument);

generateSettingsCommand.AddAlias("g");
generateSettingsCommand.SetHandler(GetDefaultSettings);

rootCommand.AddCommand(decompileCommand);
rootCommand.AddCommand(generateSettingsCommand);
rootCommand.AddCommand(disassembleCommand);

Parser parser = new CommandLineBuilder(rootCommand)
  .UseBetterDefaults(rootCommand)
  .Build();

await parser.InvokeAsync(args);
return;

void DisassembleAssembly(FileInfo assembly, DirectoryInfo outputDirectory)
{
  WriteIlSpyVersion();
  PEFile peFile = new(assembly.FullName);
  if (!Directory.Exists(outputDirectory.FullName))
    Directory.CreateDirectory(outputDirectory.FullName);

  Console.WriteLine($"Disassembling {peFile.Name}...");
  NamespaceDefinition rootNamespace = peFile.Metadata.GetNamespaceDefinitionRoot();
  DisassembleWholeNamespace(rootNamespace, outputDirectory.FullName, peFile);
  Console.WriteLine($"Done!");
}

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

  WholeProjectDecompiler decompiler = new(settings, resolver, null, null, null);

  if (!Directory.Exists(outputDirectory.FullName))
    Directory.CreateDirectory(outputDirectory.FullName);

  Console.WriteLine($"Decompiling {peFile.Name}...");
  decompiler.DecompileProject(peFile, outputDirectory.FullName);
  Console.WriteLine($"Done!");
}

void DisassembleWholeNamespace(NamespaceDefinition ns, string rootOutPath, PEFile peFile)
{
  foreach (TypeDefinitionHandle typeDefinitionHandle in ns.TypeDefinitions)
  {
    string fileName = Path.Combine(rootOutPath,
      typeDefinitionHandle.GetFullTypeName(peFile.Metadata).Name
        .Replace('<', '_')
        .Replace('>', '_') + ".il");

    Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
    using StreamWriter streamWriter = File.CreateText(fileName);
    ReflectionDisassembler disassembler = new(new PlainTextOutput(streamWriter), CancellationToken.None);
    disassembler.DisassembleType(peFile, typeDefinitionHandle);
  }

  foreach (NamespaceDefinitionHandle namespaceDefinitionHandle in ns.NamespaceDefinitions)
  {
    NamespaceDefinition namespaceDefinition = peFile.Metadata.GetNamespaceDefinition(namespaceDefinitionHandle);
    string namespaceName = peFile.Metadata.GetString(namespaceDefinition.Name);
    DisassembleWholeNamespace(namespaceDefinition, Path.Combine(rootOutPath, namespaceName), peFile);
  }
}
