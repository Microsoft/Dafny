using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DiffMatchPatch;
using Microsoft.Extensions.DependencyModel;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Assert = Xunit.Assert;

namespace DafnyTests {

    public class DafnyTests {

        private static string DAFNY_ROOT = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.Parent.FullName;
        private static string DAFNY_EXE = Path.Combine(DAFNY_ROOT, "Binaries/Dafny.exe");
        private static string TEST_ROOT = Path.Combine(DAFNY_ROOT, "Test") + Path.DirectorySeparatorChar;
        private static string COMP_DIR = Path.Combine(TEST_ROOT, "comp") + Path.DirectorySeparatorChar;
        
        public static string RunDafny(IEnumerable<string> arguments) {
            List<string> dafnyArguments = new List<string> {
                // Expected output does not contain logo
                "-nologo",
                "-countVerificationErrors:0",

                // We do not want absolute or relative paths in error messages, just the basename of the file
                "-useBaseNameForFileName",

                // We do not want output such as "Compiled program written to Foo.cs"
                // from the compilers, since that changes with the target language
                "-compileVerbose:0"
            };
            dafnyArguments.AddRange(arguments);
            
            using (Process dafnyProcess = new Process()) {
                dafnyProcess.StartInfo.FileName = "mono";
                dafnyProcess.StartInfo.ArgumentList.Add(DAFNY_EXE);
                foreach (var argument in dafnyArguments) {
                    dafnyProcess.StartInfo.ArgumentList.Add(argument);
                }

                dafnyProcess.StartInfo.UseShellExecute = false;
                dafnyProcess.StartInfo.RedirectStandardOutput = true;
                dafnyProcess.StartInfo.RedirectStandardError = true;
                dafnyProcess.StartInfo.CreateNoWindow = true;
                // Necessary for JS to find bignumber.js
                dafnyProcess.StartInfo.WorkingDirectory = TEST_ROOT;
                
                // Only preserve specific whitelisted environment variables
                dafnyProcess.StartInfo.EnvironmentVariables.Clear();
                dafnyProcess.StartInfo.EnvironmentVariables.Add("PATH", System.Environment.GetEnvironmentVariable("PATH"));
                // Go requires this or GOCACHE
                dafnyProcess.StartInfo.EnvironmentVariables.Add("HOME", System.Environment.GetEnvironmentVariable("HOME"));

                dafnyProcess.Start();
                dafnyProcess.WaitForExit();
                string output = dafnyProcess.StandardOutput.ReadToEnd();
                string error = dafnyProcess.StandardError.ReadToEnd();
                if (dafnyProcess.ExitCode != 0) {
                    Assert.True(false, output + "\n" + error);
                }

                return output + error;
            }
        }

        private static string GetTestCaseConfigString(string filePath) {
            // TODO-RS: Figure out how to do this cleanly on a TextReader instead,
            // and to handle things like nested comments.
            string fullText = File.ReadAllText(filePath);
            var commentStart = fullText.IndexOf("/*");
            if (commentStart >= 0) {
                var commentEnd = fullText.IndexOf("*/", commentStart + 2);
                if (commentEnd >= 0) {
                    var commentContent = fullText.Substring(commentStart + 2, commentEnd - commentStart - 2).Trim();
                    if (commentContent.StartsWith("---")) {
                        return commentContent;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<YamlNode> Expand(YamlNode node) {
            if (node is YamlSequenceNode seqNode) {
                return seqNode.SelectMany(child => Expand(child));
            } else if (node is YamlMappingNode mappingNode) {
                return CartesianProduct(mappingNode.Select(ExpandValue)).Select(FromPairs);
            } else {
                return new[]{ node };
            }
        }

        private static IEnumerable<KeyValuePair<YamlNode, YamlNode>> ExpandValue(KeyValuePair<YamlNode, YamlNode> pair) {
            return Expand(pair.Value).Select(v => KeyValuePair.Create(pair.Key, v));
        }

        private static YamlMappingNode FromPairs(IEnumerable<KeyValuePair<YamlNode, YamlNode>> pairs) {
            var result = new YamlMappingNode();
            foreach (var pair in pairs) {
                result.Add(pair.Key, pair.Value);
            }

            return result;
        }
        
        /**
         * Source: https://docs.microsoft.com/en-us/archive/blogs/ericlippert/computing-a-cartesian-product-with-linq
         */
        private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] {item}));
        }
        
        public static IEnumerable<object[]> AllTestFiles() {
//            var filePaths = Directory.GetFiles(TEST_ROOT, "*.dfy", SearchOption.AllDirectories)
//                                     .Select(path => GetRelativePath(TEST_ROOT, path));
            var filePaths = new[] { "dafny4/git-issue250.dfy" };
            foreach (var filePath in filePaths) {
                var fullFilePath = Path.Combine(TEST_ROOT, filePath);
                string configString = GetTestCaseConfigString(fullFilePath);
                if (configString != null) {
                    var yamlStream = new YamlStream();
                    yamlStream.Load(new StringReader(configString));
                    if (yamlStream.Documents[0].RootNode is YamlMappingNode mapping) {
                        Console.WriteLine(mapping["compile"]);
                    }
                }
                
                var languages = new string[] {"cs", "java", "go", "js"};
                foreach (var language in languages) {
                    yield return new object[]
                        {filePath, String.Join(" ", new[] {"/compile:3", "/compileTarget:" + language})};
                }
            }
        }

        // TODO-RS: Replace with Path.GetRelativePath() if we move to .NET Core 3.1
        private static string GetRelativePath(string relativeTo, string path) {
            var fullRelativeTo = Path.GetFullPath(relativeTo);
            var fullPath = Path.GetFullPath(path);
            Assert.StartsWith(fullRelativeTo, fullPath);
            return fullPath.Substring(fullRelativeTo.Length);
        }

        private static void AssertEqualWithDiff(string expected, string actual) {
            if (expected != actual) {
                DiffMatchPatch.DiffMatchPatch dmp = DiffMatchPatchModule.Default;
                List<Diff> diff = dmp.DiffMain(expected, actual);
                dmp.DiffCleanupSemantic(diff);
                string patch = DiffText(diff);
                throw new AssertActualExpectedException(expected, actual, patch);
            }
        }

        private static string DiffText(List<Diff> diffs) {
            return "";
        }
        
        [ParallelTheory]
        [MemberData(nameof(AllTestFiles))]
        public void Test(string file, string args) {
            string fullInputPath = Path.Combine(TEST_ROOT, file);
            string[] arguments = args.Split();
            
            string expectedOutputPath = fullInputPath + ".expect";
            bool specialCase = false;
            string compileTarget = arguments.FirstOrDefault(arg => arg.StartsWith("/compileTarget:"));
            if (compileTarget != null) {
                string language = compileTarget.Substring("/compileTarget:".Length);
                var specialCasePath = fullInputPath + "." + language + ".expect";
                if (File.Exists(specialCasePath)) {
                    specialCase = true;
                    expectedOutputPath = specialCasePath;
                }
            }
            string expectedOutput = File.ReadAllText(expectedOutputPath);

            string output = RunDafny(new List<string>{ file }.Concat(arguments));
            
            AssertEqualWithDiff(expectedOutput, output);
            Skip.If(specialCase, "Confirmed known exception for arguments: " + args);
        }

        [Fact]
        public void ExpandTest() {
            
        }
    }
}