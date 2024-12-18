﻿using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.ApplicationUpdate
{
    public readonly record struct DownloadProgress(double ProgressPercentage, long BytesReceived, long TotalBytesToReceive);

    public record ApplicationUpdateVersionInfo(Uri ReleaseNotesUri, string Version, string FileName);

    public class ApplicationUpdater
    {
        public IObservable<ApplicationUpdateVersionInfo> WhenUpdateAvailable => updateAvailableSubject.AsObservable();

        private readonly ISubject<ApplicationUpdateVersionInfo> updateAvailableSubject = new Subject<ApplicationUpdateVersionInfo>();

        public bool IsCheckingForUpdates { get; private set; }

        public ApplicationUpdateVersionInfo? VersionInfo { get; private set; }

        public async Task CheckForUpdatesAsync(ApplicationUpdateChannel channel, IProgress<DownloadProgress> progress)
        {
            if (IsCheckingForUpdates)
                return;

            IsCheckingForUpdates = true;

            try
            {
                VersionInfo = await DownloadUpdateAsync(channel, progress, default).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                Logger.LogException("Error while checking for application update", exception.GetInnerException());
            }

            IsCheckingForUpdates = false;

            if (VersionInfo != null)
            {
                updateAvailableSubject.OnNext(VersionInfo);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        private record GithubReleaseAsset(string name, string browser_download_url);
        private record GithubRelease(string name, GithubReleaseAsset[] assets, string html_url);
#pragma warning restore IDE1006 // Naming Styles

        public async Task<ApplicationUpdateVersionInfo?> DownloadUpdateAsync(ApplicationUpdateChannel channel, IProgress<DownloadProgress> progress, CancellationToken ct = default)
        {
            int rev = Core.Revision;
            string updateUrl = $"https://github.com/toadmaninteractive/hercules/releases/latest/download/";
            string? revConfUrl = updateUrl + "rev.conf";
            string? herculesSetupUrl = updateUrl + "hercules_setup.exe";
            string? releaseNotesUri = "https://github.com/toadmaninteractive/hercules/releases/latest";

            if (rev == 0)
                return null;

            using var httpClient = HttpClientFactory.Create();
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
            if (channel == ApplicationUpdateChannel.Beta)
            {
                var releases = await httpClient.GetFromJsonAsync<GithubRelease[]>("https://api.github.com/repos/toadmaninteractive/hercules/releases", ct);
                var latestRelease = releases!.MaxBy(r => System.Version.Parse(r.name));
                revConfUrl = latestRelease?.assets.FirstOrDefault(a => a.name == "rev.conf")?.browser_download_url;
                herculesSetupUrl = latestRelease?.assets.FirstOrDefault(a => a.name == "hercules_setup.exe")?.browser_download_url;
                releaseNotesUri = latestRelease?.html_url;
            }

            if (string.IsNullOrEmpty(revConfUrl) || string.IsNullOrEmpty(herculesSetupUrl))
                return null;

            string remoteRev = (await httpClient.GetStringAsync(revConfUrl, ct).ConfigureAwait(false)).Trim();
            if (remoteRev != rev.ToString(CultureInfo.InvariantCulture))
            {
                using HttpResponseMessage response = await httpClient.GetAsync(herculesSetupUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long? fileSize = response.Content.Headers.ContentLength;
                string tempFileName = GenerateFilename(channel, remoteRev);
                File.Delete(tempFileName);

                const int bufferSize = 65536;
                using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
                using (var fileStream = File.Create(tempFileName, bufferSize, FileOptions.Asynchronous))
                {
                    var totalRead = 0L;
                    using var buffer = MemoryPool<byte>.Shared.Rent(bufferSize);

                    do
                    {
                        var read = await contentStream.ReadAsync(buffer.Memory, ct);
                        if (read == 0)
                            break;

                        await fileStream.WriteAsync(buffer.Memory.Slice(0, read), ct);

                        totalRead += read;

                        if (fileSize.HasValue)
                        {
                            double percentage = ((double)totalRead * 100) / fileSize.Value;
                            progress.Report(new DownloadProgress(percentage, totalRead, fileSize.Value));
                        }
                    }
                    while (true);
                }

                return new ApplicationUpdateVersionInfo(new Uri(releaseNotesUri!), remoteRev, tempFileName);
            }
            else
            {
                Logger.Log("No available application update found");
                return null;
            }
        }

        private static string GenerateFilename(ApplicationUpdateChannel channel, string rev)
        {
            var tempFolder = Path.Combine(PathUtils.TempFolder, "Update");
            Directory.CreateDirectory(tempFolder);
            return Path.Combine(tempFolder, $"hercules_setup_{channel.ToTag()}_{rev}.exe");
        }
    }
}