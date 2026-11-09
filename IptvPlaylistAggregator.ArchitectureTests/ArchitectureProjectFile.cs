namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Represents a solution project file that the architecture checks examine.
    /// </summary>
    /// <param name="Name">
    /// The name of the project, which matches its project file name.
    /// </param>
    /// <param name="FullPath">
    /// The absolute path of the staged project file.
    /// </param>
    public sealed record ArchitectureProjectFile(string Name, string FullPath);
}
