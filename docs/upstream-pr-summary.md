# Upstream contribution: download pipeline hardening

Target repository: `shaked6540/YoutubePlaylistDownloader`

This document is a draft for an upstream pull request. It summarizes the
changes already reviewed and merged in the fork through PR #15. The cancelled
UI/UX redesign is intentionally not included.

## Proposed pull request summary

This contribution hardens the download pipeline without changing the existing
user-facing workflow. It makes URL lookups and FFmpeg conversion cancellable,
prevents incomplete files from replacing valid files, validates persisted
settings, reduces progress-update overhead, adds Windows CI coverage, and
removes duplicated path-building logic.

## Changes included

### URL lookup and FFmpeg lifecycle

- Debounce URL input by 300 ms.
- Cancel superseded YouTube lookups and ignore stale responses.
- Pass named cancellation tokens through YoutubeExplode calls.
- Await FFmpeg conversion tasks instead of using fire-and-forget work.
- Validate FFmpeg exit status and output existence before promoting a result.
- Terminate FFmpeg cleanly when cancellation occurs.
- Release conversion slots reliably after success, failure, or cancellation.

Source fork PR: #11 (`9fbdf08`)

### CI and updater safety

- Add GitHub Actions restore/build validation on Windows with .NET 8.
- Restrict workflow permissions to read-only repository contents.
- Download updates to a `.part` file first.
- Accept only HTTPS URLs from the official GitHub hosts.
- Validate HTTP status and reject empty/non-Windows update files.
- Promote the installer only after the complete download succeeds.
- Remove partial files after failures and cancellation.
- Replace the asynchronous `ContinueWith` chain with an awaitable flow.
- Document local build/run requirements and FFmpeg placement in `README.md`.

Source fork PR: #12 (`32374e9`)

### Atomic download and conversion outputs

- Stage video, converted audio, captions, and subtitle files as `.part` files.
- Promote completed files atomically.
- Preserve an existing valid destination when a new operation fails.
- Clean temporary files in `finally` blocks.
- Add xUnit coverage for successful promotion and failure preservation.

Source fork PR: #13 (`45fd376`)

### Settings validation and progress updates

- Normalize persisted settings and add schema-version fields.
- Validate formats, bitrate, playlist indexes, conversion limits, paths, and
  other persisted values.
- Fall back to safe defaults when stored values are invalid.
- Save settings through atomic replacement.
- Throttle intermediate progress dispatcher updates to 100 ms.
- Preserve the final 100% progress update.
- Add coverage for atomic settings-file replacement.

Source fork PR: #14 (`d43e2ae`)

### Shared download path model

- Centralize input, temporary, output, audio, caption, and destination path
  construction in `Utilities/DownloadPaths.cs`.
- Reuse the same path model in both download flows.
- Add characterization tests for generated temporary and final paths.
- Preserve existing download behavior while removing duplicated path logic.

Source fork PR: #15 (`912501d`)

## Compatibility and scope

- The application remains a Windows WPF application targeting .NET 8.
- FFmpeg is still required for video conversion and merged media output.
- Existing download settings, formats, translations, and UI behavior are kept.
- No UI redesign, theme change, or new runtime dependency is included.
- Authenticode signature verification is not included; it requires an official
  publisher certificate and a signing pipeline owned by the project.

## Validation

Run on Windows with the .NET 8 SDK and the Desktop development with .NET
workload installed:

```powershell
dotnet restore YoutubePlaylistDownloader.sln
dotnet build YoutubePlaylistDownloader.sln --configuration Debug
dotnet test YoutubePlaylistDownloader.sln --configuration Debug
```

For manual validation, place `ffmpeg.exe` beside the generated executable and
verify these flows:

1. Replace a URL while a lookup is still running; only the latest URL may
   update the page.
2. Cancel or interrupt a conversion; no partial output may replace an
   existing file.
3. Restart with invalid persisted settings; safe defaults must be loaded.
4. Download a playlist and confirm that the final progress reaches 100%.
5. Trigger an update download failure and confirm that the existing installer
   remains intact and `.part` files are removed.

## Suggested upstream submission flow

```powershell
git remote add upstream https://github.com/shaked6540/YoutubePlaylistDownloader.git
git fetch upstream
git switch -c upstream/download-pipeline-hardening upstream/master

# Apply the reviewed commits in dependency order.
git cherry-pick 10f134dbe60ded0d89724002fce10aef5aa21750
git cherry-pick 90fa5507f9316b69dc0cd594eacec4bcd1efecb0
git cherry-pick 4839e9c6c5e5d6dc97a162cf84ad709b0bf7bccf
git cherry-pick 6ab9b03f6616de52c5da4cc1e10b623f5f4f23a0
git cherry-pick e0524463114c702797c017c9a6278d312744f56d
git cherry-pick f13b27e53915b824ec23184e04166a446557c560
git cherry-pick 0b4af7b64ac5a0f0bbe84932daf5cf6b47ef7861
```

If the upstream branch has diverged, resolve conflicts one commit at a time,
run the validation commands above, push the branch to the fork, and open the
pull request against `shaked6540/YoutubePlaylistDownloader:master`.

## Suggested pull request title

`Harden download pipeline and add Windows CI coverage`
