using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Lip.Context.Implementation;
internal class GitImpl : IGit
{
    public async Task Clone(string repository, string directory, string? branch = null, int? depth = null)
    {
        await Task.Run(() =>
        {
            var cloneOptions = new CloneOptions(new FetchOptions { Depth = depth ?? 1 })
            {
                BranchName = branch,
            };
            Repository.Clone(repository, directory, cloneOptions);
        });
    }
}
