namespace Finite.App;

/// <summary>Central place for data file locations.</summary>
public static class DbPaths
{
    public static string GetDatabaseDirectory()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FiniteDesktop");
        System.IO.Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetDatabasePath() => System.IO.Path.Combine(GetDatabaseDirectory(), "finite.db");
}
