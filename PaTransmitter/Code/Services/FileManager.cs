using PaFandom.Code.Types;

namespace PaTransmitter.Code.Services
{
    /// <summary>
    /// Provides basic functions for writing and reading files.
    /// </summary>
    [LazyInstance(false)]
    public class FileManager : Singleton<FileManager>
    {
        /// <summary>
        /// Path to the application root folder.
        /// </summary>
        public string Content { get; private set; }

        public ILogger Logger { get; private set; }

        public void Setup(WebApplication app)
        {
            Logger = app.Logger;
            Content = app.Environment.ContentRootPath;
        }

        /// <summary>
        /// Creates all folders in specified path.
        /// </summary>
        public string InitializeFolders(string relativePath)
        {
            try
            {
                var path = Path.Join(Content, relativePath);
                var directory = new FileInfo(path).DirectoryName;

                Directory.CreateDirectory(directory);
                return path;
            }
            catch (Exception ex)
            {
                Logger.LogInformation("Exception in InitializeFolders().");
                Logger.LogError(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Read file and return text if file and text exist. Otherwise, return Option.None.
        /// </summary>
        public Option<string> ReadFile(string relativePath)
        {
            var path = InitializeFolders(relativePath);

            try
            {
                if (File.Exists(path))
                {
                    var data = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(data))
                        return Option<string>.None;
                    return data;
                }
                else
                {
                    File.Create(path);
                    return Option<string>.None;
                }
            }
            catch (Exception ex)
            {
                Logger.LogInformation("Exception in ReadFile().");
                Logger.LogError(ex.Message);
            }
            return Option<string>.None;
        }

        /// <summary>
        /// Write data to file with specified mode.
        /// </summary>
        public async Task WriteFile(string relativePath, string data, FileMode mode)
        {
            var path = InitializeFolders(relativePath);

            var fs = new FileStream(path, mode, FileAccess.Write);
            var sw = new StreamWriter(fs);

            await sw.WriteLineAsync(data);

            fs.Close();
        }
    }
}
