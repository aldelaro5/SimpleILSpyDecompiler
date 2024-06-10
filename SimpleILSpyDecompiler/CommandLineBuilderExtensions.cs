using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.Reflection;

namespace SimpleILSpyDecompiler;

public static class CommandLineBuilderExtensions
{
  private static readonly MethodBase s_formatArgumentUsage = typeof(HelpBuilder).GetMethod("FormatArgumentUsage",
    BindingFlags.Instance | BindingFlags.NonPublic)!;

  private static readonly MethodBase s_getCommandArgumentRows = typeof(HelpBuilder).GetMethod("GetCommandArgumentRows",
    BindingFlags.Instance | BindingFlags.NonPublic)!;

  public static CommandLineBuilder UseBetterDefaults(this CommandLineBuilder builder, RootCommand rootCommand)
  {
    return builder
      .UseVersionOption("-v", "--version")
      .UseDefaults()
      .UseHelp(ctx =>
      {
        ctx.HelpBuilder.CustomizeLayout(_ =>
        [
          helpContext => VersionInfoSection(helpContext, rootCommand),
          HelpBuilder.Default.SynopsisSection(),
          helpContext => AllCommandsUsageSection(helpContext, rootCommand),
          HelpBuilder.Default.SubcommandsSection(),
          helpContext => AllCommandsArgumentsSection(helpContext, rootCommand),
          HelpBuilder.Default.OptionsSection(),
          HelpBuilder.Default.AdditionalArgumentsSection()
        ]);
        RemoveAllArgumentsInSubcommandsSection(ctx.HelpBuilder, rootCommand);
      });
  }

  private static void VersionInfoSection(HelpContext ctx, RootCommand rootCommand)
  {
    ctx.Output.Write(rootCommand.Name);
    ctx.Output.Write(" ");
    ctx.Output.WriteLine($"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "<unknown>"}");
  }

  private static void AllCommandsArgumentsSection(HelpContext ctx, RootCommand rootCommand)
  {
    TextWriter output = ctx.Output;

    output.WriteLine(ctx.HelpBuilder.LocalizationResources.HelpArgumentsTitle());
    List<TwoColumnHelpRow> helpRows = new();
    foreach (var command in rootCommand.Subcommands.Where(x => !x.IsHidden))
    {
      var rows = (IEnumerable<TwoColumnHelpRow>)s_getCommandArgumentRows.Invoke(ctx.HelpBuilder, [command, ctx])!;
      helpRows.AddRange(rows);
    }
    ctx.HelpBuilder.WriteColumns(helpRows, ctx);
  }

  private static void RemoveAllArgumentsInSubcommandsSection(HelpBuilder helpBuilder, RootCommand rootCommand)
  {
    foreach (var command in rootCommand.Subcommands.Where(x => !x.IsHidden))
    {
      helpBuilder.CustomizeSymbol(command, ctx =>
        new string
        (
          HelpBuilder.Default
            .GetIdentifierSymbolUsageLabel(command, ctx)
            .TakeWhile(x => x != '<')
            .ToArray()
        ).Trim()
      );
    }
  }

  private static void AllCommandsUsageSection(HelpContext helpContext, RootCommand rootCommand)
  {
    TextWriter output = helpContext.Output;

    output.WriteLine(helpContext.HelpBuilder.LocalizationResources.HelpUsageTitle());
    foreach (var command in rootCommand.Subcommands.Where(x => !x.IsHidden))
    {
      output.Write("  ");
      output.Write(rootCommand.Name);
      output.Write(" ");
      output.Write(command.Name);
      if (command.Arguments.Any(x => !x.IsHidden))
      {
        output.Write(" ");
        output.Write(s_formatArgumentUsage.Invoke(helpContext.HelpBuilder, [command.Arguments]));
      }

      if (rootCommand.Options.Any(x => !x.IsHidden))
      {
        output.Write(" ");
        output.Write(helpContext.HelpBuilder.LocalizationResources.HelpUsageOptions());
      }

      output.WriteLine();
    }
  }
}
