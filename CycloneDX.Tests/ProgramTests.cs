// This file is part of CycloneDX Tool for .NET
//
// Licensed under the Apache License, Version 2.0 (the “License”);
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an “AS IS” BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// SPDX-License-Identifier: Apache-2.0
// Copyright (c) OWASP Foundation. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CycloneDX.Interfaces;
using CycloneDX.Models;
using Moq;
using Xunit;
using XFS = System.IO.Abstractions.TestingHelpers.MockUnixSupport;

namespace CycloneDX.Tests
{
    public class ProgramTests
    {
        [Fact]
        public async Task CallingCycloneDX_WithoutSolutionFile_ReturnsInvalidOptions()
        {
            var exitCode = await Program.Main(new string[] { }).ConfigureAwait(true);

            Assert.Equal((int)ExitCode.InvalidOptions, exitCode);
        }

        [Fact]
        public async Task CallingCycloneDX_CreatesOutputDirectory()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
                {
                    { XFS.Path(@"c:\SolutionPath\SolutionFile.sln"), "" }
                });
            var mockSolutionFileService = new Mock<ISolutionFileService>();
            mockSolutionFileService
                .Setup(s => s.GetSolutionDotnetDependencys(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<DotnetDependency>());

            Runner runner = new Runner(fileSystem: mockFileSystem, null, null, null, null, null, solutionFileService: mockSolutionFileService.Object, null);

            RunOptions runOptions = new RunOptions
            {
                SolutionOrProjectFile = XFS.Path(@"c:\SolutionPath\SolutionFile.sln"),
                outputDirectory = XFS.Path(@"c:\NewDirectory")
            };
            var exitCode = await runner.HandleCommandAsync(runOptions);

            Assert.Equal((int)ExitCode.OK, exitCode);
            Assert.True(mockFileSystem.FileExists(XFS.Path(@"c:\NewDirectory\bom.xml")));
        }

        [Fact]
        public async Task CallingCycloneDX_WithOutputFilename_CreatesOutputFilename()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
                {
                    { XFS.Path(@"c:\SolutionPath\SolutionFile.sln"), "" }
                });
            var mockSolutionFileService = new Mock<ISolutionFileService>();
            mockSolutionFileService
                .Setup(s => s.GetSolutionDotnetDependencys(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<DotnetDependency>());

            Runner runner = new Runner(fileSystem: mockFileSystem, null, null, null, null, null, solutionFileService: mockSolutionFileService.Object, null);

            RunOptions runOptions = new RunOptions
            {
                SolutionOrProjectFile = XFS.Path(@"c:\SolutionPath\SolutionFile.sln"),
                outputDirectory = XFS.Path(@"c:\NewDirectory"),
                outputFilename = XFS.Path(@"my_bom.xml")
            };

            var exitCode = await runner.HandleCommandAsync(runOptions);

            Assert.Equal((int)ExitCode.OK, exitCode);
            Assert.True(mockFileSystem.FileExists(XFS.Path(@"c:\NewDirectory\my_bom.xml")));
        }

        [Fact]
        public void CheckMetaDataTemplate()
        {
            var bom = new Bom();
            string resourcePath = Path.Join(AppContext.BaseDirectory, "Resources", "metadata");
            bom = Runner.ReadMetaDataFromFile(bom, Path.Join(resourcePath, "cycloneDX-metadata-template.xml"));
            Assert.NotNull(bom.Metadata);
            Assert.Matches("CycloneDX", bom.Metadata.Component.Name);
            Assert.NotEmpty(bom.Metadata.Tools.Tools);
            Assert.Matches("CycloneDX", bom.Metadata.Tools.Tools[0].Vendor);
            Assert.Matches("1.2.0", bom.Metadata.Tools.Tools[0].Version);
        }

        [Theory]
        [InlineData(@"c:\SolutionPath\SolutionFile.sln", false)]
        [InlineData(@"c:\SolutionPath\ProjectFile.csproj", false)]
        [InlineData(@"c:\SolutionPath\ProjectFile.csproj", true)]
        [InlineData(@"c:\SolutionPath\packages.config", false)]
        public async Task CallingCycloneDX_WithSolutionOrProjectFileThatDoesntExistsReturnAnythingButZero(string path, bool rs)
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
            var mockSolutionFileService = new Mock<ISolutionFileService>();
            mockSolutionFileService
                .Setup(s => s.GetSolutionDotnetDependencys(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<DotnetDependency>());

            Runner runner = new Runner(fileSystem: mockFileSystem, null, null, null, null, null, solutionFileService: mockSolutionFileService.Object, null);

            RunOptions runOptions = new RunOptions
            {
                SolutionOrProjectFile = XFS.Path(path),
                scanProjectReferences = rs,
                outputDirectory = XFS.Path(@"c:\NewDirectory"),
                outputFilename = XFS.Path(@"my_bom.xml")
            };

            var exitCode = await runner.HandleCommandAsync(runOptions);

            Assert.NotEqual((int)ExitCode.OK, exitCode);
        }

        [Fact]
        public async Task CallingCycloneDX_BurnAIAPP()
        {
            string[] args =
            [
                @"D:\AI-Burn-Commercial-App\SpectralAI.Burn.sln",
                "-t",
                "-o", @"D:\Test\Bom",
                "-fn", "SpectralAI.Burn.json"
            ];

            (int exitCode, Bom bom) = await Program.ExecuteRootCommand(args).ConfigureAwait(true);

            //await ExportBomToCsv(bom, @"D:\Test\Bom\bom.csv");
            await ExportBomToCsv(bom, @"D:\Test\Bom\SpectralAI.Burn.csv");


            Assert.Equal((int)ExitCode.OK, exitCode);
            Assert.True(File.Exists(@"D:\Test\Bom\SpectralAI.Burn.json"));
            Assert.True(File.Exists(@"D:\Test\Bom\SpectralAI.Burn.csv"));
        }

        [Fact]
        public async Task CallingCycloneDX_ImagingAPP()
        {
            string[] args =
            [
                @"D:\DV-Imaging-App\SpectralMD.ImagingApp.sln",
                "-t",
                "-o", @"D:\Test\Bom",
                "-fn", "SpectralMD.ImagingApp.json"
            ];

            (int exitCode, Bom bom) = await Program.ExecuteRootCommand(args).ConfigureAwait(true);

            await ExportBomToCsv(bom, @"D:\Test\Bom\SpectralMD.ImagingApp.csv");


            Assert.Equal((int)ExitCode.OK, exitCode);
            Assert.True(File.Exists(@"D:\Test\Bom\SpectralMD.ImagingApp.json"));
            Assert.True(File.Exists(@"D:\Test\Bom\SpectralMD.ImagingApp.csv"));
        }


        // Please try to avoid using var in the below method to make it easier to read
        private static async Task ExportBomToCsv(Bom bom, string csvPath)
        {
            List<string> lines =
            [
                // Header
                "PURL,Hashcode,Name,Version,Release Date,Supplier Name,Description,Licenses,Relationship,Level of Support,End of Support Date,External References",
            ];

            // Build a lookup for dependencies
            Dictionary<string, List<string>> dependencyMap = [];
            if (bom.Dependencies != null)
            {
                foreach (Dependency dep in bom.Dependencies)
                {
                    if (string.Equals(dep.Ref, @"SpectralAI.Burn@0.0.0") || string.Equals(dep.Ref, @"SpectralMD.ImagingApp@0.0.0"))
                    {
                        // Skip main project reference
                        continue;
                    }
                    if (dep.Dependencies != null)
                    {
                        foreach (Dependency dependsOn in dep.Dependencies)
                        {
                            string dependsOnRef = dependsOn.Ref;
                            if (string.Equals(dependsOnRef, dep.Ref, StringComparison.OrdinalIgnoreCase))
                            {
                                // Skip self-references
                                continue;
                            }
                            if (!dependencyMap.TryGetValue(dependsOnRef, out var value))
                            {
                                value = [];
                                dependencyMap[dependsOnRef] = value;
                            }

                            value.Add(dep.Ref);
                        }
                    }
                }
            }

            IEnumerable<Component> components = bom.Components ?? Enumerable.Empty<Component>();
            foreach (Component component in components)
            {
                string purl = component.BomRef ?? "";
                string hascode = (component.Hashes != null && component.Hashes.Count > 0) ? component.Hashes[0].Content ?? "" : "";
                string name = component.Name ?? "";
                string version = component.Version ?? "";
                string releaseDate = component.Properties?.FirstOrDefault(p => p.Name == "nuget:published")?.Value ?? "";
                releaseDate = DateTime.TryParse(releaseDate, out DateTime parsedDate)
                    ? parsedDate.ToString("M/d/yyyy")
                    : "";
                string authors = (component.Authors != null && component.Authors.Count > 0)
                    ? string.Join(";", component.Authors.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n)))
                    : "";
                string description = component.Description ?? "";
                string licenseUrls = (component.Licenses != null && component.Licenses.Count > 0)
                    ? string.Join(";", component.Licenses.Select(l => l.License.Url).Where(u => !string.IsNullOrEmpty(u)))
                    : "";

                // Relationship: find which components include this one
                string relationship = string.Empty;
                if (dependencyMap.TryGetValue(component.BomRef, out List<string> parents) && parents.Count > 0)
                {
                    relationship = $"Included in {string.Join(";", parents)}";
                }
                relationship = string.IsNullOrEmpty(relationship) ? "Primary" : relationship;

                string supportLevel = string.Empty;
                string endOfSupportDate = string.Empty;
                if (DateTime.TryParse(component.Properties?.FirstOrDefault(p => p.Name == "nuget:latestPublished")?.Value, out DateTime latestReleaseDate))
                {
                    // If support date is in the future, then it's still supported
                    if ((DateTime.Now - latestReleaseDate).Days > 365 * 2)
                    {
                        supportLevel = "no longer maintained";
                        endOfSupportDate = latestReleaseDate.ToString("M/d/yyyy");
                    }
                    else
                    {
                        supportLevel = "actively maintained";
                    }
                }
                else
                {
                    supportLevel = "unknown support";
                }

                string externalRefs = (component.ExternalReferences != null && component.ExternalReferences.Count > 0)
                    ? string.Join(";", component.ExternalReferences.Select(er => $"{er.Url}").Where(r => !string.IsNullOrEmpty(r)))
                    : "";

                // Escape commas in fields
                string[] fields =
                [
                    purl, hascode, name, version, releaseDate, authors, description, licenseUrls, relationship, supportLevel, endOfSupportDate, externalRefs
                ];
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = $"\"{fields[i].Replace("\"", "\"\"")}\"";
                }

                lines.Add(string.Join(",", fields));
            }

            await File.WriteAllLinesAsync(csvPath, lines);
        }

        private static async Task ExportBomToCsv_ForDE(Bom bom, string csvPath)
        {
            List<string> lines =
            [
                // Header
                "PURL,Name,Version,Nuget_Authors,Nuget_Tags,Nuget_Publisher,Nuget_Owners,Nuget_CPE",
            ];

            IEnumerable<Component> components = bom.Components ?? Enumerable.Empty<Component>();
            foreach (Component component in components)
            {
                string purl = component.BomRef ?? "";
                string name = component.Name ?? "";
                string version = component.Version ?? "";
                string authors = (component.Authors != null && component.Authors.Count > 0)
                    ? string.Join(";", component.Authors.Select(a => a.Name).Where(n => !string.IsNullOrEmpty(n)))
                    : "";
                string tags = (component.Properties?.FirstOrDefault(p => p.Name == "nuget:tags")?.Value) ?? "";
                string publisher = component.Publisher ?? "";
                string owners = component.Properties?.FirstOrDefault(p => p.Name == "nuget:owners")?.Value ?? "";
                string cpe = component.Cpe ?? "";

                // Escape commas in fields
                string[] fields =
                [
                    purl, name, version, authors, tags, publisher, owners, cpe
                ];
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = $"\"{fields[i].Replace("\"", "\"\"")}\"";
                }

                lines.Add(string.Join(",", fields));
            }

            await File.WriteAllLinesAsync(csvPath, lines);
        }
    }
}
