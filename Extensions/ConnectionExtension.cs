using System.Data;
using Dapper;
using Npgsql;

namespace JKTankDataMigration.Extensions;

public static class ConnectionExtension
{
    public static void ExecuteAllTexts(this NpgsqlConnection cn, string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            if (cn.State == ConnectionState.Closed)
                cn.Open();

            using var fs = File.OpenText(path);
            var lineData = fs.ReadLine();
            using var writer = cn.BeginTextImport(lineData!);

            lineData = fs.ReadLine();

            while (!string.IsNullOrWhiteSpace(lineData))
            {
                writer.WriteLine(lineData);
                lineData = fs.ReadLine();
            }

            Console.WriteLine($"{path} Done!");
        }
        catch (Exception e)
        {
            Console.WriteLine(path);
            Console.WriteLine(e);

            File.AppendAllText($"{Setting.INSERT_DATA_PATH}/Error/{DateTime.Now:yyyyMMdd}", path);
            File.AppendAllText($"{Setting.INSERT_DATA_PATH}/Error/{DateTime.Now:yyyyMMdd}", e.ToString());

            //throw;
        }
    }

    public static void ExecuteAllCopyFiles(this NpgsqlConnection cn, string path)
    {
        if (!Directory.Exists(path))
            return;

        var inputFilePaths = Directory.GetFiles(path, "*.sql", SearchOption.AllDirectories);

        foreach (var inputFilePath in inputFilePaths)
        {
            cn.ExecuteAllTexts(inputFilePath);
        }

        Console.WriteLine($"{path} Execute copy command done");
    }

    public static async Task ExecuteCommandByPathAsync(this NpgsqlConnection cn, string path, CancellationToken token)
    {
        if (cn.State == ConnectionState.Closed)
            await cn.OpenAsync(token);

        var commandSql = await File.ReadAllTextAsync(path, token);

        Console.WriteLine($"Start {path}");
        await cn.ExecuteAsync(commandSql);
        Console.WriteLine($"Finish {path}");
    }

    public static void ExecuteCommandByPath(this NpgsqlConnection cn, string path)
    {
        if (cn.State == ConnectionState.Closed)
            cn.Open();

        var commandSql = File.ReadAllText(path);

        Console.WriteLine($"Start {path}");
        cn.Execute(commandSql);
        Console.WriteLine($"Finish {path}");
    }
}