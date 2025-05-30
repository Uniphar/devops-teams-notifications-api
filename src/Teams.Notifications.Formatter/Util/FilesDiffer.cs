namespace Teams.Notifications.Formatter.Util;

internal sealed class FilesDiffer(string BasePath)
{
    private List<FileDiff> Diffs { get; } = new();

    public void Add(string path, Action<string, Stream> generateExpectedContents)
    {
        Diffs.Add(new FileDiff
        {
            SourcePath = Path.GetRelativePath(BasePath, path),
            SourceHash = File.Exists(path)
                ? Hash.File(path)
                : null,
            GenerateExpectedContents = generateExpectedContents
        });
    }

    public void AddAllUnderPath(string path, string pattern, Action<string, Stream> generateExpectedContents)
    {
        foreach (var file in Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories))
            Add(file, generateExpectedContents);
    }

    public bool Apply()
    {
        var success = true;
        foreach (var diff in Diffs)
        {
            using var expectedContents = GenerateAndHash(diff, out var newHash);

            if (expectedContents is null || newHash is null)
            {
                success = false;
                continue;
            }

            if (diff.SourceHash == newHash)
            {
                ConsoleFeedback.Success(diff.SourcePath);
                continue;
            }

            using var sourceFile = File.Open(diff.SourcePath, FileMode.Create);
            expectedContents.CopyTo(sourceFile);

            ConsoleFeedback.Updated(diff.SourcePath);
        }

        return success;
    }

    public bool Check(string operation, string messageWhenDifferent)
    {
        var success = true;
        foreach (var diff in Diffs)
        {
            using var expectedContents = GenerateAndHash(diff, out var newHash, operation);

            if (expectedContents is null || newHash is null)
            {
                success = false;
                continue;
            }

            if (diff.SourceHash == newHash)
                ConsoleFeedback.Success(diff.SourcePath);
            else
            {
                success = false;
                ConsoleFeedback.Error(diff.SourcePath, messageWhenDifferent);
                GitHubActions.Error(operation, messageWhenDifferent, diff.SourcePath);
            }
        }

        return success;
    }


    private static FileStream? GenerateAndHash(FileDiff diff, out string? hash, string? operation = null)
    {
        var tempFile = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            diff.GenerateExpectedContents(diff.SourcePath, tempFile);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);

            if (operation is not null && ex is JsonException jsonEx)
                GitHubActions.Error(operation, jsonEx.Message, diff.SourcePath, jsonEx.LineNumber);
            hash = null;
            return null;
        }

        tempFile.Flush();
        tempFile.Position = 0;

        hash = Hash.Stream(tempFile);
        return tempFile;
    }

    private record struct FileDiff
    {
        public required string SourcePath { get; init; }
        public string? SourceHash { get; init; }
        public required Action<string, Stream> GenerateExpectedContents { get; init; }
    }
}