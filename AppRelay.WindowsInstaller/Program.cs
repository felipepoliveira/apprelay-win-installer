using AppRelay.WindowsInstaller.Http;
using System;
using System.IO;
using System.IO.Compression;

namespace AppRelay.WindowsInstaller
{
    internal class Program
    {
        /// <summary>
        /// Create a temporary file with a random name with the given prefix.
        /// </summary>
        /// <param name="suffix">String containing the file extension, for example: ".csv"</param>
        /// <returns></returns>
        public static FileStream CreateTempFile(string fileName, string suffix)
        {
            var filePath = System.IO.Path.GetTempPath() + fileName + suffix;
            return new FileStream(filePath, FileMode.Create);
        }

        /// <summary>
        /// This method create an new directory in temporary files directory. If the directory already exists it will be deleted
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static string CreateDirectoryAndResetIfExists(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, true);
            }
            Directory.CreateDirectory(dirPath);
            return dirPath;
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        internal static int Main(string[] args)
        {
            // Create a temp file
            var tempFileStream = CreateTempFile("apprelay-download", ".zip");

            // Download the content of the file
            Console.WriteLine("Downloaded to file: " + tempFileStream.Name);
            HttpDownload.DownloadToStream(
                new Uri("https://github.com/felipepoliveira/apprelay/releases/latest/download/apprelay.zip"),
                tempFileStream,
                1024 * 16,
                (int downloadedChunk, long totalDownloaded, long contentLength) =>
                {
                    Console.WriteLine($"Downloaded {downloadedChunk} bytes of {totalDownloaded}/{contentLength}");
                }
                );

            // Decompress the content of the zip file
            Console.WriteLine("=====Download completo=====");
            Console.WriteLine("=====Descomprimindo conteúdo do download=====");
            var tmpInstallationDirPath = CreateDirectoryAndResetIfExists(System.IO.Path.GetTempPath() + "apprelay-download");
            ZipFile.ExtractToDirectory(tempFileStream.Name, tmpInstallationDirPath);
            Console.WriteLine("Conteudo descomprimido em: " + tmpInstallationDirPath);
            Console.WriteLine("=====Descompressão realizada com sucesso=====");

            // Check if the create directory contains the \bin directory
            if (!Directory.Exists(tmpInstallationDirPath + @"\bin"))
            {
                Console.WriteLine("Could not found required directory: " + (tmpInstallationDirPath + @"\bin"));
                return 5000;
            }

            // Create the application folder on %user%\AppData\Local
            var installationDirPath = CreateDirectoryAndResetIfExists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\AppRelay");
            Console.WriteLine("=====Copiando arquivos=====");
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(tmpInstallationDirPath, "*", SearchOption.AllDirectories))
            {
                var replacedDirPath = dirPath.Replace(tmpInstallationDirPath, installationDirPath);
                Console.WriteLine(replacedDirPath);
                Directory.CreateDirectory(replacedDirPath);
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(tmpInstallationDirPath, "*.*", SearchOption.AllDirectories))
            {
                var replacedNewPath = newPath.Replace(tmpInstallationDirPath, installationDirPath);
                Console.WriteLine(replacedNewPath);
                File.Copy(newPath, replacedNewPath, true);
            }
            Console.WriteLine("=====Copia de arquivos completa=====");

            // Edit the PATH environment variable only if is not already set
            var name = "PATH";
            var installationBinPath = $@";{installationDirPath}\bin";
            var scope = EnvironmentVariableTarget.User; // or User
            var oldValue = Environment.GetEnvironmentVariable(name, scope);
            if (!oldValue.Contains(installationBinPath))
            {
                var newValue = oldValue + $@";{installationDirPath}\bin";
                Console.WriteLine("Editando variável de ambiente PATH:" + newValue);
                Environment.SetEnvironmentVariable(name, newValue, scope);
            }

            // Installation completed
            return 0;
        }
    }
}
