using System.Runtime.InteropServices;
using Flurl;
using Lip.Context;
using Semver;
using SharpCompress.Archives;

namespace Lip;

/// <summary>
/// The main class of the Lip library.
/// </summary>
public partial class Lip
{
    private record PackageInstallDetail : TopoSortedPackageList<PackageInstallDetail>.IItem
    {
        public Dictionary<PackageSpecifierWithoutVersion, SemVersionRange> Dependencies
        {
            get
            {
                return Manifest.GetSpecifiedVariant(
                        VariantLabel,
                        RuntimeInformation.RuntimeIdentifier)?
                        .Dependencies?
                        .Select(
                            kvp => new KeyValuePair<PackageSpecifierWithoutVersion, SemVersionRange>(
                                PackageSpecifierWithoutVersion.Parse(kvp.Key),
                                SemVersionRange.ParseNpm(kvp.Value)))
                        .ToDictionary()
                        ?? [];
            }
        }

        public required IFileSource FileSource { get; init; }

        public required PackageManifest Manifest { get; init; }

        public PackageSpecifier Specifier => new()
        {
            ToothPath = Manifest.ToothPath,
            VariantLabel = VariantLabel,
            Version = Manifest.Version
        };

        public required string VariantLabel { get; init; }
    }

    private readonly CacheManager _cacheManager;
    private readonly IContext _context;
    private readonly DependencySolver _dependencySolver;
    private readonly PackageManager _packageManager;
    private readonly PathManager _pathManager;
    private readonly RuntimeConfig _runtimeConfig;

    public Lip(RuntimeConfig runtimeConfig, IContext context)
    {
        _context = context;
        _runtimeConfig = runtimeConfig;

        _pathManager = new(context.FileSystem, baseCacheDir: runtimeConfig.Cache, workingDir: context.WorkingDir);

        List<Url> gitHubProxies = runtimeConfig.GitHubProxies.ConvertAll(url => new Url(url));
        List<Url> goModuleProxies = runtimeConfig.GoModuleProxies.ConvertAll(url => new Url(url));

        _cacheManager = new(_context, _pathManager, gitHubProxies, goModuleProxies);

        _packageManager = new(_context, _cacheManager, _pathManager, goModuleProxies);

        _dependencySolver = new(_context, _cacheManager, _packageManager);
    }

    private async Task<PackageInstallDetail> GetFileSourceFromUserInputPackageText(string userInputPackageText)
    {
        // First, check if package text refers to a local directory containing a tooth.json file.

        string possibleDirPath = _context.FileSystem.Path.Join(_pathManager.WorkingDir, userInputPackageText.Split('#')[0]);

        if (_context.FileSystem.Directory.Exists(possibleDirPath))
        {
            DirectoryFileSource directoryFileSource = new(_context.FileSystem, possibleDirPath);

            PackageManifest? packageManifest = await _packageManager.GetPackageManifestFromFileSource(directoryFileSource);

            if (packageManifest is not null)
            {
                return new()
                {
                    FileSource = directoryFileSource,
                    Manifest = packageManifest,
                    VariantLabel = userInputPackageText.Split('#').ElementAtOrDefault(1) ?? string.Empty
                };
            }
        }

        // Second, check if package text refers to a local archive file.

        string possibleFilePath = _context.FileSystem.Path.Join(_pathManager.WorkingDir, userInputPackageText.Split('#')[0]);

        if (_context.FileSystem.File.Exists(possibleFilePath))
        {
            using Stream fileStream = _context.FileSystem.File.OpenRead(possibleFilePath);

            if (ArchiveFactory.IsArchive(fileStream, out _))
            {
                ArchiveFileSource archiveFileSource = new(_context.FileSystem, possibleFilePath);

                PackageManifest? packageManifest = await _packageManager.GetPackageManifestFromFileSource(archiveFileSource);

                if (packageManifest is not null)
                {
                    return new()
                    {
                        FileSource = archiveFileSource,
                        Manifest = packageManifest,
                        VariantLabel = userInputPackageText.Split('#').ElementAtOrDefault(1) ?? string.Empty
                    };
                }
            }
        }

        // Third, assume package text is a package specifier.

        {
            var packageSpecifier = PackageSpecifier.Parse(userInputPackageText);

            IFileSource fileSource = await _cacheManager.GetPackageFileSource(packageSpecifier);

            PackageManifest? packageManifest = await _packageManager.GetPackageManifestFromFileSource(fileSource);

            if (packageManifest is not null)
            {
                return new()
                {
                    FileSource = fileSource,
                    Manifest = packageManifest,
                    VariantLabel = packageSpecifier.VariantLabel
                };
            }
        }

        // If none of the above, throw an exception.

        throw new ArgumentException($"Cannot resolve package text '{userInputPackageText}'.", nameof(userInputPackageText));
    }
}
