using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace engenious.ContentTool.SourceGen
{
    public static class SourceExtensions
    {
        private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

        public static string GetMSBuildProperty(
            this GeneratorExecutionContext context,
            string name,
            string defaultValue = "")
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
            return value ?? defaultValue;
        }

        public static IEnumerable<string> GetMSBuildItems(this GeneratorExecutionContext context, string name)
            => context
                .AdditionalFiles
                .Where(f => context.AnalyzerConfigOptions
                                .GetOptions(f)
                                .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                            && sourceItemGroup == name)
                .Select(f => f.Path);
    }
}