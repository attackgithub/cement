﻿using System.IO;
using Common;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.UtilsTests
{
    [TestFixture]
    public class TestInstallCollector
    {
        [Test]
        public void TestGetExternalInstalls()
        {
            var text = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext";
            var result = YamlFromText.InstallParser(text).Get().ExternalModules;
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("ext", result[0]);
        }

        [Test]
        public void TestGetNuGetInstalls()
        {
            var text = @"
full-build:
    deps:
        - ext
    install:
        - current
        - nuget Newtonsoft.Json";
            var result = YamlFromText.InstallParser(text).Get().NuGetPackages;
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Newtonsoft.Json", result[0]);
        }

        private void CreateModule(string moduleName, string content)
        {
            if (!Directory.Exists(moduleName))
            {
                Directory.CreateDirectory(moduleName);
            }
            var filePath = Path.Combine(moduleName, "module.yaml");
            File.WriteAllText(filePath, content);
        }

        [Test]
        public void TestWithExternals()
        {
            var externalModuleText = @"
full-build:
    install:
        - external
        - nuget pExternal
";
            var moduleText = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext
        - nuget pCurrent";
            using (var tempDir = new TempDirectory())
            {
                using (new DirectoryJumper(tempDir.Path))
                {
                    CreateModule("ext", externalModuleText);
                    CreateModule("cur", moduleText);
                    var installData = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                    var buildFiles = installData.BuildFiles.ToArray();
                    var nugetPackages = installData.NuGetPackages.ToArray();
                    Assert.AreEqual(new[] {@"cur\current", @"ext\external"}, buildFiles);
                    Assert.AreEqual(new[] {"pCurrent", "pExternal"}, nugetPackages);
                }
            }
        }

        [Test]
        public void TestCollectInstallWithExternalClientConfig()
        {
            var externalModuleText = @"
full-build:
    install:
        - external
client:
    install:
        - external.client
";
            var moduleText = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext/client
";
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                CreateModule("ext", externalModuleText);
                CreateModule("cur", moduleText);
                var result = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                Assert.AreEqual(new[] {@"cur\current", @"ext\external.client"}, result.BuildFiles.ToArray());
            }
        }

        [Test]
        public void TestLongNestingsWithConfigs()
        {
            var qText = @"
full-build:
    install:
sdk:
    install:
        - q.sdk
        - module ext/client
";
            var externalModuleText = @"
full-build:
    install:
        - external
        - module q/sdk
client:
    install:
        - external.client
";
            var moduleText = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext
";
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                CreateModule("q", qText);
                CreateModule("ext", externalModuleText);
                CreateModule("cur", moduleText);
                var result = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                Assert.AreEqual(new[] {@"cur\current", @"ext\external", @"q\q.sdk", @"ext\external.client"},
                    result.BuildFiles.ToArray());
            }
        }

        [Test]
        public void TestLongNestingsWithConfigsNexting()
        {
            var qText = @"
full-build:
    install:
sdk:
    install:
        - q.sdk
        - module ext/client
        - nuget q.sdk
";
            var externalModuleText = @"
full-build:
    install:
        - external
        - module q/sdk
        - nuget external
client:
    install:
        - external.client
        - nuget external.client
";
            var moduleText = @"
full-build > client:
    deps:
        - ext
    install:
        - current
        - module ext
        - nuget current
client:
    install:
        - current.client
        - module ext/client
        - nuget client
";
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                CreateModule("q", qText);
                CreateModule("ext", externalModuleText);
                CreateModule("cur", moduleText);
                var installData = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                Assert.AreEqual(
                    new[] {@"cur\current", @"cur\current.client", @"ext\external", @"ext\external.client", @"q\q.sdk"},
                    installData.BuildFiles.ToArray());
                Assert.AreEqual(
                    new[] { "current", "client", "external", "external.client", "q.sdk" },
                    installData.NuGetPackages.ToArray());
            }
        }
    }
}