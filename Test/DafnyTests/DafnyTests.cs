using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace DafnyTests {

    public class TestData : IEnumerable<object[]> {
        public IEnumerator<object[]> GetEnumerator() {
            var filePaths = new string[] {"comp/Hello.dfy"};
            var languages = new string[] {"cs", "java", "go", "js"};
            foreach (var filePath in filePaths) {
                foreach (var language in languages) {
                    yield return new object[] { filePath, language };
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    public class DafnyTests {

        private static string DAFNY_ROOT = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName;
        private static string TEST_ROOT = Path.Combine(DAFNY_ROOT, "Test");

        private string RunDafnyProgram(string filePath, params string[] otherArguments) {
            using (Process dafnyProcess = new Process()) {
                dafnyProcess.StartInfo.FileName = "dafny";
                dafnyProcess.StartInfo.Arguments = Path.Combine(TEST_ROOT, filePath);
                dafnyProcess.StartInfo.Arguments += " /compile:3";
                // Expected output does not contain logo
                dafnyProcess.StartInfo.Arguments += " -nologo -countVerificationErrors:0";

                // We do not want absolute or relative paths in error messages, just the basename of the file
                dafnyProcess.StartInfo.Arguments += " -useBaseNameForFileName";

                // We do not want output such as "Compiled program written to Foo.cs"
                // from the compilers, since that changes with the target language
                dafnyProcess.StartInfo.Arguments += " -compileVerbose:0";
                
                foreach (var otherArgument in otherArguments) {
                    dafnyProcess.StartInfo.Arguments += " " + otherArgument;
                }
                dafnyProcess.StartInfo.UseShellExecute = false;
                dafnyProcess.StartInfo.RedirectStandardOutput = true;
                dafnyProcess.StartInfo.RedirectStandardError = true;
                dafnyProcess.StartInfo.CreateNoWindow = true;
                dafnyProcess.StartInfo.WorkingDirectory = TEST_ROOT;
                dafnyProcess.Start();
                dafnyProcess.WaitForExit();
                if (dafnyProcess.ExitCode != 0) {
                    Assert.True(false, dafnyProcess.StandardError.ReadToEnd());
                }
                return dafnyProcess.StandardOutput.ReadToEnd();
            }
        }
        
        [SkippableTheory()]
        [ClassData(typeof(TestData))]
        public void ValidProgramOutput(String inputPath, String targetLanguage) {
            string output = RunDafnyProgram(inputPath, "/compileTarget:" + targetLanguage);
            string expectedOutput = File.ReadAllText(Path.Combine(TEST_ROOT, inputPath + ".expect"));
            Assert.Equal(expectedOutput, output);
        }
    }
}