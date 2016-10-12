using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using FlubuCore.Context;

namespace FlubuCore.Tasks.FileSystem
{
    /// <summary>
    ///     Copies a directory tree from the source to the destination.
    /// </summary>
    public class CopyDirectoryStructureTask : TaskBase
    {
        private readonly string _destinationPath;
        private readonly bool _overwriteExisting;
        private readonly string _sourcePath;
        private List<string> _copiedFilesList;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CopyDirectoryStructureTask" /> class
        ///     using a specified source and destination path and an indicator whether to overwrite existing files.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <param name="overwriteExisting">if set to <c>true</c> the task will overwrite existing destination files.</param>
        public CopyDirectoryStructureTask(string sourcePath, string destinationPath, bool overwriteExisting)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
            _overwriteExisting = overwriteExisting;
        }

        /// <summary>
        ///     Gets the list of all destination files that were copied.
        /// </summary>
        /// <value>The list of all destination files that were copied.</value>
        public IList<string> CopiedFilesList => _copiedFilesList;

        /// <summary>
        ///     Gets or sets the exclusion regular expression pattern for files.
        /// </summary>
        /// <remarks>
        ///     All files whose paths match this regular expression
        ///     will not be copied. If the <see cref="ExclusionPattern" /> is <c>null</c>, it will be ignored.
        /// </remarks>
        /// <value>The exclusion pattern.</value>
        public string ExclusionPattern { get; set; }

        /// <summary>
        ///     Gets or sets the inclusion regular expression pattern for files.
        /// </summary>
        /// <remarks>
        ///     All files whose paths match this regular expression
        ///     will be copied. If the <see cref="InclusionPattern" /> is <c>null</c>, it will be ignored.
        /// </remarks>
        /// <value>The inclusion pattern.</value>
        public string InclusionPattern { get; set; }

        /// <summary>
        ///     Copies a directory tree from the source to the destination.
        /// </summary>
        /// <param name="context">The script execution environment.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <param name="overwriteExisting">if set to <c>true</c> the task will overwrite existing destination files.</param>
        public static void Execute(
            ITaskContext context,
            string sourcePath,
            string destinationPath,
            bool overwriteExisting)
        {
            var task = new CopyDirectoryStructureTask(sourcePath, destinationPath, overwriteExisting);
            task.Execute(context);
        }

        /// <summary>
        ///     Internal task execution code.
        /// </summary>
        /// <param name="context">The script execution environment.</param>
        protected override int DoExecute(ITaskContext context)
        {
            context.LogInfo($"Copy directory structure from '{_sourcePath}' to '{_destinationPath}");
            _copiedFilesList = new List<string>();

            Regex inclusionRegex = null;
            if (InclusionPattern != null)
                inclusionRegex = new Regex(InclusionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            Regex exclusionRegex = null;
            if (ExclusionPattern != null)
                exclusionRegex = new Regex(ExclusionPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            CopyStructureRecursive(context, _sourcePath, _destinationPath, inclusionRegex, exclusionRegex);

            return 0;
        }

        private void CopyStructureRecursive(
            ITaskContext context,
            string sourcePathRecursive,
            string destinationPathRecursive,
            Regex inclusionRegex,
            Regex exclusionRegex)
        {
            if ((exclusionRegex != null) && exclusionRegex.IsMatch(sourcePathRecursive))
                return;

            var info = new DirectoryInfo(sourcePathRecursive);

            foreach (var fileSystemInfo in info.GetFileSystemInfos())
            {
                if (fileSystemInfo is FileInfo)
                {
                    if ((inclusionRegex != null) && (!inclusionRegex.IsMatch(fileSystemInfo.FullName)))
                        continue;
                    if ((exclusionRegex != null) && exclusionRegex.IsMatch(fileSystemInfo.FullName))
                        continue;

                    var fileInfo = fileSystemInfo as FileInfo;
                    var filePath = Path.Combine(destinationPathRecursive, fileInfo.Name);

                    if (!Directory.Exists(destinationPathRecursive))
                        Directory.CreateDirectory(destinationPathRecursive);

                    fileInfo.CopyTo(filePath, _overwriteExisting);
                    context.LogInfo($"Copied file '{fileSystemInfo.FullName}' to '{filePath}'");
                    _copiedFilesList.Add(filePath);
                }
                else
                {
                    var dirInfo = fileSystemInfo as DirectoryInfo;
                    var subdirectoryPath = Path.Combine(
                        destinationPathRecursive,
                        dirInfo.Name);
                    CopyStructureRecursive(
                        context,
                        dirInfo.FullName,
                        subdirectoryPath,
                        inclusionRegex,
                        exclusionRegex);
                }
            }
        }
    }
}