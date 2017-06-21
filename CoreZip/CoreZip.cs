using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Simplified.IO
{
    public static class CoreZip
    {
        #region Enums
        /// <summary>
        /// Used to specify how to proceed
        /// when the destination zip file already exists.
        /// </summary>
        public enum ExistingArchiveAction
        {
            /// <summary>
            /// Update the zip file.
            /// </summary>
            Update,
            /// <summary>
            /// Replace the zip file.
            /// </summary>
            Replace,
            /// <summary>
            /// Throw an error.
            /// </summary>
            Error,
            /// <summary>
            /// Ignore/ dont create a new zip file.
            /// </summary>
            Ignore
        }

        /// <summary>
        /// Used to specify what the overwrite policy
        /// is for items being extracted.
        /// </summary>
        public enum Overwrite
        {
            Always,
            IfNewer,
            Never
        }
        #endregion

        #region Puplic Methods
        /// <summary>
        /// Allows you to add items to an archive, whether the archive
        /// already exists or not
        /// </summary>
        /// <param name="directoryToZip"></param>
        /// <param name="archiveFilePath">
        /// The destination archive/zip file path.
        /// </param>
        /// <param name="objectsToArchive">
        /// A set of file names that are to be added
        /// </param>
        /// <param name="action">
        /// Specifies how we are going to handle an existing archive
        /// </param>
        /// <param name="fileOverwrite"></param>
        /// <param name="compression">
        /// Specifies what type of compression to use - defaults to Optimal
        /// </param>
        public static void Compress(string directoryToZip, string archiveFilePath, ExistingArchiveAction action = ExistingArchiveAction.Replace, Overwrite fileOverwrite = Overwrite.IfNewer, CompressionLevel compression = CompressionLevel.Optimal)
        {
            try
            {
                // Identifies the mode we will be using - the default is Create
                var mode = ZipArchiveMode.Create;

                // Determines if the zip file even exists
                var archiveExists = File.Exists(archiveFilePath);

                // Figures out what to do based upon our specified overwrite method
                switch (action)
                {
                    case ExistingArchiveAction.Update:
                        // Sets the mode to update if the file exists, otherwise
                        // the default of Create is fine
                        if (archiveExists)
                            mode = ZipArchiveMode.Update;
                        break;
                    case ExistingArchiveAction.Replace:
                        // Deletes the file if it exists.  Either way, the default
                        // mode of Create is fine
                        if (archiveExists)
                            File.Delete(archiveFilePath);
                        break;
                    case ExistingArchiveAction.Error:
                        // Throws an error if the file exists
                        if (archiveExists)
                            throw new IOException($"The zip file {archiveFilePath} already exists.");
                        break;
                    case ExistingArchiveAction.Ignore:
                        // Closes the method silently and does nothing
                        if (archiveExists)
                            return;
                        break;
                    default:
                        break;
                }

                // Opens the zip file in the mode we specified
                using (var zipFile = ZipFile.Open(archiveFilePath, mode))
                {
                    // Determine the parent directory from which we will translate
                    // paths. 
                    var parentDir = Directory.GetParent(directoryToZip).ToString();

                    // Fetch all the file and folder objects from the target directory
                    var objectsToArchive = new List<string>();
                    var directories = Directory.GetDirectories(directoryToZip, "*.*", SearchOption.AllDirectories);
                    var files = Directory.GetFiles(directoryToZip, "*.*", SearchOption.AllDirectories);
                    foreach (var dir in directories)
                        objectsToArchive.Add(dir + "\\");
                    foreach (var file in files)
                        objectsToArchive.Add(file);

                    if (mode == ZipArchiveMode.Create)
                        foreach (var file in objectsToArchive)
                        {
                            // Translate the path to the zip path
                            var translatedZipFilePath = file.Replace(parentDir, string.Empty);
                            if (translatedZipFilePath.StartsWith("\\"))
                                translatedZipFilePath = translatedZipFilePath.TrimStart('\\');

                            // This supports a directory or a file.
                            zipFile.AddEntry(file, translatedZipFilePath, compression);
                        }
                    // Update
                    else
                        foreach (var file in objectsToArchive)
                        {
                            var translatedZipFilePath = file.Replace(parentDir, string.Empty);
                            if (translatedZipFilePath.StartsWith("\\"))
                                translatedZipFilePath = translatedZipFilePath.TrimStart('\\');

                            var fileFound = (from f in zipFile.Entries where f.FullName == translatedZipFilePath select f)
                                .FirstOrDefault();

                            switch (fileOverwrite)
                            {
                                case Overwrite.Always:
                                    // Deletes the file if it is found
                                    fileFound?.Delete();

                                    // Adds the file to the archive
                                    zipFile.AddEntry(file, translatedZipFilePath, compression);

                                    break;
                                case Overwrite.IfNewer:
                                    // Only delete the file if it is
                                    // newer, but if it is newer or if the file isn't already in
                                    // the zip file, write it to the zip file
                                    if (fileFound != null)
                                    {
                                        // Deletes the file only if it is older than our file.
                                        // Note that the file will be ignored if the existing file
                                        // in the archive is newer.
                                        if (fileFound.LastWriteTime < File.GetLastWriteTime(file))
                                        {
                                            fileFound.Delete();

                                            // Adds the file to the archive
                                            zipFile.AddEntry(file, translatedZipFilePath, compression);
                                        }
                                    }
                                    else
                                    {
                                        // The file does not exist so add it to the archive.
                                        zipFile.AddEntry(file, translatedZipFilePath, compression);
                                    }
                                    break;
                                case Overwrite.Never:
                                    // Don't do anything
                                    break;
                                default:
                                    break;
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error compressing archive: " + RecurseExceptionText(ex));
            }
        }


        /// <summary>
        /// Unzips the specified file to the given folder in a safe
        /// manner.  This plans for missing paths and existing items
        /// and handles them gracefully.
        /// </summary>
        /// <param name="sourceZipFilePath">
        /// The name of the zip file to be extracted
        /// </param>
        /// <param name="destinationDirectoryName">
        /// The directory to extract the zip file to
        /// </param>
        /// <param name="overwriteMethod">
        /// Specifies how we are going to handle an existing file.
        /// The default is IfNewer.
        /// </param>
        public static void Uncompress(string sourceZipFilePath, 
            string destinationDirectoryName,
            Overwrite overwriteMethod = Overwrite.IfNewer)
        {
            try
            {
                // Opens the zip file up to be read
                using (var archive = ZipFile.OpenRead(sourceZipFilePath))
                {
                    // Loops through each file in the zip file
                    foreach (var file in archive.Entries)
                        ExtractToFile(file, destinationDirectoryName, overwriteMethod);
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error un-compressing archive: " + RecurseExceptionText(ex));
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Extracts a single file from a zip file
        /// </summary>
        /// <param name="file">
        /// The zip entry we are pulling the file from
        /// </param>
        /// <param name="destinationPath">
        /// The root of where the file is going
        /// </param>
        /// <param name="overwriteMethod">
        /// Specifies how we are going to handle an existing file.
        /// The default is Overwrite.IfNewer.
        /// </param>
        private static void ExtractToFile(ZipArchiveEntry file,
            string destinationPath,
            Overwrite overwriteMethod = Overwrite.IfNewer)
        {
            // Gets the complete path for the destination file, including any
            // relative paths that were in the zip file
            var destinationFileName = Path.Combine(destinationPath, file.FullName);

            // Gets just the new path, minus the file name so we can create the
            // directory if it does not exist
            var destinationFilePath = Path.GetDirectoryName(destinationFileName);

            // Creates the directory (if it doesn't exist) for the new path
            Directory.CreateDirectory(destinationFilePath);

            // The zipfile is a directory. 
            if (file.ToString().EndsWith("\\"))
                return;

            // Determines what to do with the file based upon the
            // method of overwriting chosen
            switch (overwriteMethod)
            {
                case Overwrite.Always:
                    // Just put the file in and overwrite anything that is found
                    file.ExtractToFile(destinationFileName, true);
                    break;
                case Overwrite.IfNewer:
                    // Checks to see if the file exists, and if so, if it should
                    // be overwritten
                    if (!File.Exists(destinationFileName) || File.GetLastWriteTime(destinationFileName) < file.LastWriteTime)
                        file.ExtractToFile(destinationFileName, true);
                    break;
                case Overwrite.Never:
                    // Put the file in if it is new but ignores the 
                    // file if it already exists
                    if (!File.Exists(destinationFileName))
                        file.ExtractToFile(destinationFileName);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Helpers

        private static void AddEntry(this ZipArchive zipArchive, string file, string translatedZipFilePath, CompressionLevel compression)
        {
            if (file.EndsWith("\\"))
                zipArchive.CreateEntry(translatedZipFilePath, compression);
            else
                zipArchive.CreateEntryFromFile(file, translatedZipFilePath, compression);
        }

        private static string RecurseExceptionText(Exception ex)
        {
            string exText = string.Empty;

            if (ex != null)
            {
                exText = ex.Message + " \r\n";

                if (ex.InnerException != null)
                    exText += RecurseExceptionText(ex.InnerException);
            }

            return exText;
        }

        #endregion
    }
}