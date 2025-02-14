using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Lip;

public partial class Lip
{
    public record UpdateArgs
    {
        public required bool DryRun { get; init; }
        public required bool IgnoreScripts { get; init; }
        public required bool NoDependencies { get; init; }
        public required bool Save { get; init; }
    }

    public async Task Update(List<string> userInputPackageTexts, UpdateArgs args)
    {
        // Parse user input package texts for primary packages and check conflicts.

        TopoSortedPackageList<PackageInstallDetail> packageInstallDetails = [];

        List<PackageSpecifierWithoutVersion> packageSpecifiersToUninstall = [];

        foreach (string packageText in userInputPackageTexts)
        {
            PackageInstallDetail installDetail = await GetFileSourceFromUserInputPackageText(packageText);

            PackageManifest? installedPackageManifest = await _packageManager.GetPackageManifestFromInstalledPackages(
                installDetail.Specifier)
                ?? throw new InvalidOperationException(
                    $"Package '{installDetail.Manifest.ToothPath}' is not installed.");

            // If installed with the same version, skip.
            if (installedPackageManifest.Version == installDetail.Manifest.Version)
            {
                _context.Logger.LogWarning("Package '{specifier}' is already installed with the same version. Skipping.", new PackageSpecifier()
                {
                    ToothPath = installDetail.Manifest.ToothPath,
                    VariantLabel = installDetail.VariantLabel,
                    Version = installDetail.Manifest.Version
                });

                continue;
            }

            // Otherwise, need reinstall.
            packageInstallDetails.Add(installDetail);

            packageSpecifiersToUninstall.Add(new PackageSpecifierWithoutVersion()
            {
                ToothPath = installDetail.Manifest.ToothPath,
                VariantLabel = installDetail.VariantLabel
            });
        }

        // Solve dependencies.

        List<PackageSpecifier> dependencyPackageSpecifiers = args.NoDependencies
            ? []
            : await _dependencySolver.GetDependencies(
                packageInstallDetails.ConvertAll(detail => new PackageSpecifier()
                {
                    ToothPath = detail.Manifest.ToothPath,
                    VariantLabel = detail.VariantLabel,
                    Version = detail.Manifest.Version
                }));

        foreach (PackageSpecifier dependencyPackageSpecifier in dependencyPackageSpecifiers)
        {
            PackageManifest? installedPackageManifest = await _packageManager.GetPackageManifestFromInstalledPackages(
                dependencyPackageSpecifier);

            // If installed with the same version, skip.
            if (installedPackageManifest?.Version == dependencyPackageSpecifier.Version)
            {
                continue;
            }

            // If installed with different version, uninstall.
            if (installedPackageManifest is not null && installedPackageManifest.Version != dependencyPackageSpecifier.Version)
            {
                packageSpecifiersToUninstall.Add(new()
                {
                    ToothPath = dependencyPackageSpecifier.ToothPath,
                    VariantLabel = dependencyPackageSpecifier.VariantLabel
                });
            }

            // Add to install details.

            IFileSource fileSource = await _cacheManager.GetPackageFileSource(dependencyPackageSpecifier);

            PackageManifest packageManifest = await _packageManager.GetPackageManifestFromFileSource(fileSource)
                ?? throw new InvalidOperationException($"Cannot get package manifest from package '{dependencyPackageSpecifier}'.");

            packageInstallDetails.Add(new PackageInstallDetail()
            {
                FileSource = fileSource,
                Manifest = packageManifest,
                VariantLabel = dependencyPackageSpecifier.VariantLabel
            });
        }

        // Uninstall packages.

        foreach (PackageSpecifierWithoutVersion packageSpecifier in packageSpecifiersToUninstall)
        {
            await _packageManager.UninstallPackage(packageSpecifier, args.DryRun, args.IgnoreScripts);
        }

        // Install packages.

        foreach (PackageInstallDetail packageInstallDetail in packageInstallDetails)
        {
            PackageSpecifier packageSpecifier = new()
            {
                ToothPath = packageInstallDetail.Manifest.ToothPath,
                VariantLabel = packageInstallDetail.VariantLabel,
                Version = packageInstallDetail.Manifest.Version
            };

            // Lock the package if it is not a dependency.
            await _packageManager.InstallPackage(
                packageInstallDetail.FileSource,
                packageInstallDetail.VariantLabel,
                args.DryRun,
                args.IgnoreScripts,
                locked: !dependencyPackageSpecifiers.Contains(packageSpecifier));
        }

        // If to save, update the package manifest file.

        if (args.Save)
        {
            PackageManifest packageManifest = await _packageManager.GetCurrentPackageManifestWithTemplate()
                ?? throw new InvalidOperationException("Package manifest is not found.");

            PackageManifest newPackagemanifest = packageManifest with
            {
                Variants = packageManifest.Variants?
                    .ConvertAll(variant =>
                    {
                        if (!variant.Match(
                            string.Empty,
                            RuntimeInformation.RuntimeIdentifier))
                        {
                            return variant;
                        }

                        if (variant.Dependencies is null)
                        {
                            return variant;
                        }

                        PackageManifest.VariantType newVariant = variant with { };

                        foreach (PackageInstallDetail installDetail in packageInstallDetails)
                        {
                            PackageSpecifierWithoutVersion packageSpecifier = new()
                            {
                                ToothPath = installDetail.Manifest.ToothPath,
                                VariantLabel = installDetail.VariantLabel
                            };

                            newVariant.Dependencies![packageSpecifier.ToString()] = installDetail.Manifest.Version.ToString();
                        }

                        return newVariant;
                    })
            };

            if (!args.DryRun)
            {
                await _packageManager.SaveCurrentPackageManifest(newPackagemanifest);
            }
        }
    }
}
