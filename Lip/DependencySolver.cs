using System.Runtime.InteropServices;
using Algorithms.Graphs;
using DataStructures.Graphs;
using Lip.Context;

namespace Lip;

public class DependencySolver(
    IContext context,
    CacheManager cacheManager,
    PackageManager packageManager)
{
    private readonly CacheManager _cacheManager = cacheManager;
    private readonly IContext _context = context;
    private readonly PackageManager _packageManager = packageManager;

    public async Task<List<PackageSpecifier>> GetDependencies(List<PackageSpecifier> primaryPackageSpecifiers)
    {
        // TODO: Implement this method.
        return [];
    }

    public async Task<List<PackageSpecifierWithoutVersion>> GetUnnecessaryPackages()
    {
        PackageLock packageLock = await _packageManager.GetCurrentPackageLock();

        List<ComparablePackageSpecifierWithoutVersion> necessaryPackages = [.. packageLock.Locks
            .Where(@lock => @lock.Locked)
            .Select(@lock => new ComparablePackageSpecifierWithoutVersion
            {
                ToothPath = @lock.Package.ToothPath,
                VariantLabel = @lock.VariantLabel
            })];

        DirectedSparseGraph<ComparablePackageSpecifierWithoutVersion> dependencyGraph = new();

        dependencyGraph.AddVertices([.. necessaryPackages.Cast<ComparablePackageSpecifierWithoutVersion>()]);

        // Add edges.
        foreach (ComparablePackageSpecifierWithoutVersion packageSpecifier in necessaryPackages)
        {
            PackageManifest packageManifest = (await _packageManager.GetPackageManifestFromInstalledPackages(packageSpecifier))!;

            IEnumerable<ComparablePackageSpecifierWithoutVersion> dependencies = packageManifest.GetSpecifiedVariant(
                packageSpecifier.VariantLabel,
                RuntimeInformation.RuntimeIdentifier)?
                .Dependencies?
                .Select(dep => PackageSpecifierWithoutVersion.Parse(dep.Key))
                .Cast<ComparablePackageSpecifierWithoutVersion>() ?? [];

            foreach (ComparablePackageSpecifierWithoutVersion dependency in dependencies)
            {
                dependencyGraph.AddEdge(packageSpecifier, dependency);
            }
        }

        // Find unnecessary packages.
        List<PackageSpecifierWithoutVersion> unnecessaryPackages = [.. ConnectedComponents.Compute(dependencyGraph)
            .Where(component => !component.Any(package => necessaryPackages.Contains(package)))
            .SelectMany(component => component)
            .Cast<PackageSpecifierWithoutVersion>()];

        return unnecessaryPackages;
    }
}

file record ComparablePackageSpecifierWithoutVersion : PackageSpecifierWithoutVersion, IComparable<ComparablePackageSpecifierWithoutVersion>
{
    // C-Sharp-Algorithms requires this method to be implemented but we don't know why.
    public int CompareTo(ComparablePackageSpecifierWithoutVersion? other) => throw new NotImplementedException();
}
