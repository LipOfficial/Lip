using System;
using Flurl;

namespace Lip.Context.Implementation;

internal class DownloaderImpl : IDownloader
{
    public async Task DownloadFile(Url url, string destinationPath)
    {
        Stream? output = null;
        HttpClient? client = null;

        try
        {
            byte[] buffer = new byte[1024];
            client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url.ToString(), HttpCompletionOption.ResponseHeadersRead);

            Stream input = await response.Content.ReadAsStreamAsync();

            output = File.Create(destinationPath);

            int totalBytesRead = 0, bytesRead = 0;
            while (true)
            {
                bytesRead = await input.ReadAsync(buffer);

                if (bytesRead is 0) break;

                totalBytesRead += bytesRead;

                DownloadProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs(
                    totalBytesRead,
                    response.Content.Headers.ContentLength ?? 0));

                await output.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
        }
        finally
        {
            output?.Dispose();
            client?.Dispose();
        }


    }

    public record DownloadProgressChangedEventArgs(long BytesDownloaded, long TotalBytesToDownload);

    public event EventHandler<DownloadProgressChangedEventArgs>? DownloadProgressChanged;
}
