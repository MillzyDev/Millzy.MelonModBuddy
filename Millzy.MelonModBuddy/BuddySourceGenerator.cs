/*
 *      Copyright (c) 2026 Millzy
 *
 *      Licensed under the Apache License, Version 2.0 (the "License");
 *      you may not use this file except in compliance with the License.
 *      You may obtain a copy of the License at
 *
 *          http://www.apache.org/licenses/LICENSE-2.0
 *
 *      Unless required by applicable law or agreed to in writing, software
 *      distributed under the License is distributed on an "AS IS" BASIS,
 *      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *      See the License for the specific language governing permissions and
 *      limitations under the License.
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Millzy.MelonModBuddy
{
    [Generator]
    public sealed class BuddySourceGenerator : IIncrementalGenerator
    {
        private const string generatedClassName = "ModInfo";

        private static readonly DiagnosticDescriptor MissingRequiredPropertyError = new DiagnosticDescriptor(
            id: "UMB001",
            title: "Missing Required Property",
            messageFormat: "The required MSBuild property {0} is not defined.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
        
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Root namespace generated build info will go in
            IncrementalValueProvider<AnalyzerConfigOptionsProvider> optionsProvider =
                context.AnalyzerConfigOptionsProvider;

            IncrementalValueProvider<GeneratorProperties> generatorPropertiesProvider = optionsProvider
                .Select((options, cancellationToken) =>
                {
                    AnalyzerConfigOptions globalOptions = options.GlobalOptions;
                    string rootNamespace = globalOptions.TryGetValue("build_property.RootNamespace", out string? rootNamespaceProp)
                        ? rootNamespaceProp
                        : "";
                    
                    string modInfoNamespace =
                        globalOptions.TryGetValue("build_property.ModInfoNamespace", out string? modInfoNamespaceProp) && modInfoNamespaceProp != ""
                            ? modInfoNamespaceProp
                            : rootNamespace;
                    
                    string modInfoClassName =
                        globalOptions.TryGetValue("build_property.ModInfoClassName", out string? modInfoClassNameProp) && modInfoClassNameProp != ""
                            ? modInfoClassNameProp
                            : generatedClassName;
                    
                    globalOptions.TryGetValue("build_property.ModName", out string? modName); // must be defined
                    globalOptions.TryGetValue("build_property.ModAuthor", out string? modAuthor); // must be defined
                    globalOptions.TryGetValue("build_property.ModVersion", out string? modVersion); // must be defined
                    
                    string modId = 
                        globalOptions.TryGetValue("build_property.ModId", out string? modIdProp) && modIdProp != ""
                            ? modIdProp
                            : $"{modAuthor}.{modName}";
                    
                    bool modApplyMelonAttributes =
                        globalOptions.TryGetValue("build_property.ModApplyMelonAttributes",
                            out string? modApplyMelonAttributesProp) && modApplyMelonAttributesProp.ToLower() == "true";
                    
                    bool modUseMelonId =
                        globalOptions.TryGetValue("build_property.ModUseMelonId",
                            out string? modUseMelonIdProp) && modUseMelonIdProp.ToLower() == "true";
                    
                    string modClassName = globalOptions.TryGetValue("build_property.ModClassName", out string? modClassNameProp) && modIdProp != ""
                            ? modClassNameProp : "Mod";
                    globalOptions.TryGetValue("build_property.MelonGameDeveloper", out string? melonGameDeveloper);
                    globalOptions.TryGetValue("build_property.MelonGameName", out string? melonGameName);

                    return new GeneratorProperties
                    {
                        RootNamespace = rootNamespace,
                        
                        ModInfoNamespace = modInfoNamespace,
                        ModInfoClassName = modInfoClassName,

                        Name = modName,
                        Id = modId,
                        Author = modAuthor,
                        Version = modVersion,

                        ApplyMelonAttributes = modApplyMelonAttributes,
                        UseMelonId = modUseMelonId,
                        ModClassName = modClassName,
                        MelonGameDeveloper = melonGameDeveloper,
                        MelonGameName = melonGameName,
                    };
                });
            
            context.RegisterSourceOutput(generatorPropertiesProvider, (spc, generatorProperties) =>
            {
                bool errorDiagnosed = false;
                
                if (generatorProperties.Name is null or "")
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(MissingRequiredPropertyError, Location.None, "ModName")
                    );
                    errorDiagnosed = true;
                }
                if (generatorProperties.Author is null or "")
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(MissingRequiredPropertyError, Location.None, "ModAuthor")
                    );
                    errorDiagnosed = true;
                }
                if (generatorProperties.Version is null or "")
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(MissingRequiredPropertyError, Location.None, "ModVersion")
                    );
                    errorDiagnosed = true;
                }

                if (errorDiagnosed)
                {
                    return;
                }
                
                // No errors we can continue
                string source = $$"""
                                  namespace {{generatorProperties.ModInfoNamespace}} {
                                      internal static class {{generatorProperties.ModInfoClassName}} {
                                          public const string Name = "{{generatorProperties.Name}}";
                                          public const string Id = "{{generatorProperties.Id}}";
                                          public const string Author = "{{generatorProperties.Author}}";
                                          public const string Version = "{{generatorProperties.Version}}";
                                      }
                                  }
                                  """;
                
                spc.AddSource($"{generatorProperties.ModInfoClassName}.g.cs", source);
            });
            
            context.RegisterSourceOutput(generatorPropertiesProvider, (spc, generatorProperties) =>
            {
                if (!generatorProperties.ApplyMelonAttributes)
                {
                    return;
                }

                string usings = $"""
                                  using System;
                                  using MelonLoader;
                                  using {generatorProperties.RootNamespace};
                                  """;

                string melonMod = $"""
                                   [assembly: MelonInfo(
                                       typeof({generatorProperties.ModClassName}), 
                                       {generatorProperties.ModInfoNamespace}.{generatorProperties.ModInfoClassName}.Name,
                                       {generatorProperties.ModInfoNamespace}.{generatorProperties.ModInfoClassName}.Version,
                                       {generatorProperties.ModInfoNamespace}.{generatorProperties.ModInfoClassName}.Author
                                   )]
                                   """;

                string melonId = !generatorProperties.UseMelonId 
                    ? "" : $"[assembly: MelonID({generatorProperties.ModInfoNamespace}.{generatorProperties.ModInfoClassName}.Id)]";

                string melonGameDeveloper =
                    generatorProperties.MelonGameDeveloper is null or "" ||
                    generatorProperties.MelonGameName is null or ""
                        ? ""
                        : $"""
                           [assembly: MelonGame("{generatorProperties.MelonGameDeveloper}", "{generatorProperties.MelonGameName}")]
                           """;

                string source = $"""
                                 {usings}
                                 
                                 {melonMod}
                                 {melonId}
                                 {melonGameDeveloper}
                                 """;
                
                spc.AddSource("MelonAssemblyInfo.g.cs", source);
            });
        }
    }
}