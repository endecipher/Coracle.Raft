﻿#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=GitReleaseNotes"
#addin "nuget:?package=Cake.Json"
#addin nuget:?package=Newtonsoft.Json&version=9.0.1

// compile
var compileConfig = Argument("configuration", "Release");
var slnFile = "./Rafty.sln";

// build artifacts
var artifactsDir = Directory("artifacts");

// unit testing
var artifactsForUnitTestsDir = artifactsDir + Directory("UnitTests");
var unitTestAssemblies = @"./test/Rafty.UnitTests/Rafty.UnitTests.csproj";

// acceptance testing
var artifactsForAcceptanceTestsDir = artifactsDir + Directory("AcceptanceTests");
var acceptanceTestAssemblies = @"./test/Rafty.AcceptanceTests/Rafty.AcceptanceTests.csproj";

// integration testing
var artifactsForIntegrationTestsDir = artifactsDir + Directory("IntegrationTests");
var integrationTestAssemblies = @"./test/Rafty.IntegrationTests/Rafty.IntegrationTests.csproj";

// benchmark testing
var artifactsForBenchmarkTestsDir = artifactsDir + Directory("BenchmarkTests");
var benchmarkTestAssemblies = @"./test/Rafty.Benchmarks";

// packaging
var packagesDir = artifactsDir + Directory("Packages");
var releaseNotesFile = packagesDir + File("releasenotes.md");
var artifactsFile = packagesDir + File("artifacts.txt");

// unstable releases
var nugetFeedUnstableKey = EnvironmentVariable("nuget-apikey-unstable");
var nugetFeedUnstableUploadUrl = "https://www.nuget.org/api/v2/package";
var nugetFeedUnstableSymbolsUploadUrl = "https://www.nuget.org/api/v2/package";

// stable releases
var tagsUrl = "https://api.github.com/repos/tompallister/rafty/releases/tags/";
var nugetFeedStableKey = EnvironmentVariable("nuget-apikey-stable");
var nugetFeedStableUploadUrl = "https://www.nuget.org/api/v2/package";
var nugetFeedStableSymbolsUploadUrl = "https://www.nuget.org/api/v2/package";

// internal build variables - don't change these.
var releaseTag = "";
string committedVersion = "0.0.0-dev";
var buildVersion = committedVersion;
GitVersion versioning = null;
var nugetFeedUnstableBranchFilter = "^(develop)$|^(PullRequest/)";

var target = Argument("target", "Default");


Information("target is " +target);
Information("Build configuration is " + compileConfig);	

Task("Default")
	.IsDependentOn("Build");

Task("Build")
	.IsDependentOn("RunTests")
	.IsDependentOn("CreatePackages");

Task("BuildAndReleaseUnstable")
	.IsDependentOn("Build")
	.IsDependentOn("ReleasePackagesToUnstableFeed");
	
Task("Clean")
	.Does(() =>
	{
        if (DirectoryExists(artifactsDir))
        {
            DeleteDirectory(artifactsDir, recursive:true);
        }
        CreateDirectory(artifactsDir);
	});
	
Task("Version")
	.Does(() =>
	{
		versioning = GetNuGetVersionForCommit();
		var nugetVersion = versioning.NuGetVersion;
		Information("SemVer version number: " + nugetVersion);

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			Information("Persisting version number...");
			PersistVersion(nugetVersion);
			buildVersion = nugetVersion;
		}
		else
		{
			Information("We are not running on build server, so we won't persist the version number.");
		}
	});

Task("Restore")
	.IsDependentOn("Clean")
	.IsDependentOn("Version")
	.Does(() =>
	{	
		DotNetCoreRestore(slnFile);
	});

Task("Compile")
	.IsDependentOn("Restore")
	.Does(() =>
	{	
		var settings = new DotNetCoreBuildSettings
		{
			Configuration = compileConfig,
		};
		
		DotNetCoreBuild(slnFile, settings);
	});

Task("RunUnitTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		var settings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
		};

		EnsureDirectoryExists(artifactsForUnitTestsDir);
		DotNetCoreTest(unitTestAssemblies, settings);
	});

Task("RunAcceptanceTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		var settings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
		};

		EnsureDirectoryExists(artifactsForAcceptanceTestsDir);
		DotNetCoreTest(acceptanceTestAssemblies, settings);
	});

Task("RunIntegrationTests")
	.IsDependentOn("Compile")
	.Does(() =>
	{
		var settings = new DotNetCoreTestSettings
		{
			Configuration = compileConfig,
		};

		EnsureDirectoryExists(artifactsForIntegrationTestsDir);
		DotNetCoreTest(integrationTestAssemblies, settings);
	});

Task("RunTests")
	.IsDependentOn("RunUnitTests")
	.IsDependentOn("RunAcceptanceTests")
	.IsDependentOn("RunIntegrationTests");

Task("CreatePackages")
	.IsDependentOn("Compile")
	.Does(() => 
	{
		EnsureDirectoryExists(packagesDir);
		CopyFiles("./src/**/Rafty.*.nupkg", packagesDir);

		//GenerateReleaseNotes();

        System.IO.File.WriteAllLines(artifactsFile, new[]{
            "nuget:Rafty." + buildVersion + ".nupkg",
            //"releaseNotes:releasenotes.md"
        });

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			var path = packagesDir.ToString() + @"/**/*";

			foreach (var file in GetFiles(path))
			{
				AppVeyor.UploadArtifact(file.FullPath);
			}
		}
	});

Task("ReleasePackagesToUnstableFeed")
	.IsDependentOn("CreatePackages")
	.Does(() =>
	{
		if (ShouldPublishToUnstableFeed())
		{
			PublishPackages(nugetFeedUnstableKey, nugetFeedUnstableUploadUrl, nugetFeedUnstableSymbolsUploadUrl);
		}
	});

Task("EnsureStableReleaseRequirements")
    .Does(() =>
    {
        if (!AppVeyor.IsRunningOnAppVeyor)
		{
           throw new Exception("Stable release should happen via appveyor");
		}
        
		var isTag =
           AppVeyor.Environment.Repository.Tag.IsTag &&
           !string.IsNullOrWhiteSpace(AppVeyor.Environment.Repository.Tag.Name);

        if (!isTag)
		{
           throw new Exception("Stable release should happen from a published GitHub release");
		}
    });

Task("UpdateVersionInfo")
    .IsDependentOn("EnsureStableReleaseRequirements")
    .Does(() =>
    {
        releaseTag = AppVeyor.Environment.Repository.Tag.Name;
        AppVeyor.UpdateBuildVersion(releaseTag);
    });

Task("DownloadGitHubReleaseArtifacts")
    .IsDependentOn("UpdateVersionInfo")
    .Does(() =>
    {
		try
		{
			Information("DownloadGitHubReleaseArtifacts");

			EnsureDirectoryExists(packagesDir);

			Information("Directory exists...");

			var releaseUrl = tagsUrl + releaseTag;

			Information("Release url " + releaseUrl);

			//var releaseJson = Newtonsoft.Json.Linq.JObject.Parse(GetResource(releaseUrl));            

        	var assets_url = Newtonsoft.Json.Linq.JObject.Parse(GetResource(releaseUrl))
				.GetValue("assets_url")
				.Value<string>();

			Information("Assets url " + assets_url);

			var assets = GetResource(assets_url);

			Information("Assets " + assets_url);

			foreach(var asset in Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(assets))
			{
				Information("In the loop..");

				var file = packagesDir + File(asset.Value<string>("name"));

				Information("Downloading " + file);
				
				DownloadFile(asset.Value<string>("browser_download_url"), file);
			}

			Information("Out of the loop...");
		}
		catch(Exception exception)
		{
			Information("There was an exception " + exception);
			throw;
		}
    });

Task("ReleasePackagesToStableFeed")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
    {
		PublishPackages(nugetFeedStableKey, nugetFeedStableUploadUrl, nugetFeedStableSymbolsUploadUrl);
    });

Task("Release")
    .IsDependentOn("ReleasePackagesToStableFeed");

RunTarget(target);

/// Gets nuique nuget version for this commit
private GitVersion GetNuGetVersionForCommit()
{
    GitVersion(new GitVersionSettings{
        UpdateAssemblyInfo = false,
        OutputType = GitVersionOutput.BuildServer
    });

    return GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });
}

/// Updates project version in all of our projects
private void PersistVersion(string version)
{
	Information(string.Format("We'll search all csproj files for {0} and replace with {1}...", committedVersion, version));

	var projectFiles = GetFiles("./**/*.csproj");

	foreach(var projectFile in projectFiles)
	{
		var file = projectFile.ToString();
 
		Information(string.Format("Updating {0}...", file));

		var updatedProjectFile = System.IO.File.ReadAllText(file)
			.Replace(committedVersion, version);

		System.IO.File.WriteAllText(file, updatedProjectFile);
	}
}

/// generates release notes based on issues closed in GitHub since the last release
private void GenerateReleaseNotes()
{
	Information("Generating release notes at " + releaseNotesFile);

    var releaseNotesExitCode = StartProcess(
        @"./tools/GitReleaseNotes/tools/gitreleasenotes.exe", 
        new ProcessSettings { Arguments = ". /o " + releaseNotesFile });

    if (string.IsNullOrEmpty(System.IO.File.ReadAllText(releaseNotesFile)))
	{
        System.IO.File.WriteAllText(releaseNotesFile, "No issues closed since last release");
	}

    if (releaseNotesExitCode != 0) 
	{
		throw new Exception("Failed to generate release notes");
	}
}

/// Publishes code and symbols packages to nuget feed, based on contents of artifacts file
private void PublishPackages(string feedApiKey, string codeFeedUrl, string symbolFeedUrl)
{
        var artifacts = System.IO.File
            .ReadAllLines(artifactsFile)
            .Select(l => l.Split(':'))
            .ToDictionary(v => v[0], v => v[1]);

		var codePackage = packagesDir + File(artifacts["nuget"]);

        NuGetPush(
            codePackage,
            new NuGetPushSettings {
                ApiKey = feedApiKey,
                Source = codeFeedUrl
            });
}


/// gets the resource from the specified url
private string GetResource(string url)
{
	try
	{
		Information("Getting resource from " + url);

		var assetsRequest = System.Net.WebRequest.CreateHttp(url);
		assetsRequest.Method = "GET";
		assetsRequest.Accept = "application/vnd.github.v3+json";
		assetsRequest.UserAgent = "BuildScript";

		using (var assetsResponse = assetsRequest.GetResponse())
		{
			var assetsStream = assetsResponse.GetResponseStream();
			var assetsReader = new StreamReader(assetsStream);
			var response =  assetsReader.ReadToEnd();

			Information("Response is " + response);
			
			return response;
		}
	}
	catch(Exception exception)
	{
		Information("There was an exception " + exception);
		throw;
	}
}

private bool ShouldPublishToUnstableFeed()
{
	var regex = new System.Text.RegularExpressions.Regex(nugetFeedUnstableBranchFilter);
	var publish = regex.IsMatch(versioning.BranchName);
	if (publish)
	{
		Information("Branch " + versioning.BranchName + " will be published to the unstable feed");
	}
	else
	{
		Information("Branch " + versioning.BranchName + " will not be published to the unstable feed");
	}
	return publish;	
}