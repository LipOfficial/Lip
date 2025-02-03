using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lip.Context.Implementation;

internal class ContextImpl : IContext
{
    public IDownloader Downloader => new DownloaderImpl();

    public IFileSystem FileSystem => new FileSystem();

    public IGit? Git => new GitImpl();

    public required ILogger Logger { get; set; }

    public required IUserInteraction UserInteraction { get; set; }
}
