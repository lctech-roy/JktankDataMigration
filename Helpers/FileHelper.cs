using System.Text;
using JKTankDataMigration.Extensions;
using Npgsql;

namespace JKTankDataMigration.Helpers;

public static class FileHelper
{
    private static UTF8Encoding _encoding = new UTF8Encoding(true, false);

    public static async Task CombineMultipleFilesIntoSingleFileAsync(string inputDirectoryPath, string inputFileNamePattern, string outputFilePath, CancellationToken cancellationToken = default)
    {
        var inputFilePaths = Directory.GetFiles(inputDirectoryPath, inputFileNamePattern, SearchOption.AllDirectories);
        Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);
        await using var outputStream = File.Create(outputFilePath);

        foreach (var inputFilePath in inputFilePaths)
        {
            await using (var inputStream = File.OpenRead(inputFilePath))
            {
                // Buffer size can be passed as the second argument.
                await inputStream.CopyToAsync(outputStream, cancellationToken);
            }

            File.Delete(inputFilePath);

            Console.WriteLine("The file {0} has been processed.", inputFilePath);
        }
    }

    public static void ExecuteAllSqlFiles(string inputDirectoryPath, string connectionStr)
    {
        if (!Directory.Exists(inputDirectoryPath))
            return;

        var inputFilePaths = Directory.GetFiles(inputDirectoryPath, "*.sql", SearchOption.AllDirectories).OrderBy(x => x).ToArray();

        var totalFileCount = inputFilePaths.Length;

        Console.WriteLine("Number of files: {0}.", totalFileCount);

        using var connection = new NpgsqlConnection(connectionStr);

        foreach (var inputFilePath in inputFilePaths)
        {
            Console.WriteLine("Number of files left: {0}.", totalFileCount--);

            connection.ExecuteAllTexts(inputFilePath);
        }

        //
        // Parallel.ForEach(inputFilePaths, CommonHelper.GetParallelOptions(CancellationToken.None),
        //                  inputFilePath =>
        //                  {
        //                      using var connection = new NpgsqlConnection(connectionStr);
        //
        //                      connection.ExecuteAllTexts(inputFilePath);
        //                  });
    }

    public static void WriteToFile(string directoryPath, string fileName, string copyPrefix, StringBuilder valueSb, bool isAppend = false)
    {
        if (valueSb.Length == 0)
            return;

        var fullPath = $"{directoryPath}/{fileName}";

        switch (isAppend)
        {
            case true when File.Exists(fullPath):
                File.AppendAllText(fullPath, valueSb.ToString(), _encoding);

                Console.WriteLine($"Append {fullPath}");

                break;
            case true when !File.Exists(fullPath):
                Directory.CreateDirectory(directoryPath);
                File.WriteAllText(fullPath, string.Concat(copyPrefix, valueSb.ToString()), _encoding);

                Console.WriteLine(fullPath);

                break;
            default:
                Directory.CreateDirectory(directoryPath);
                File.WriteAllText(fullPath, string.Concat(copyPrefix, valueSb.ToString()), _encoding);

                Console.WriteLine(fullPath);

                break;
        }

        valueSb.Clear();
    }

    public static void RemoveFilesByDate(IEnumerable<string> rootPaths, string dateFolderName)
    {
        var periods = PeriodHelper.GetPeriods(dateFolderName);

        foreach (var rootPath in rootPaths)
        {
            foreach (var period in periods)
            {
                var path = $"{rootPath}/{period.FolderName}";

                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
        }
    }

    public static void RemoveFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}