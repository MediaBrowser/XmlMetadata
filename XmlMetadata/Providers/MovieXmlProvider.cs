﻿using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

using System.IO;
using System.Threading;
using XmlMetadata.Parsers;

namespace XmlMetadata.Providers
{
    public class MovieXmlProvider : BaseXmlProvider<Movie>
    {
        private readonly IProviderManager _providerManager;
        

        public MovieXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            
        }

        protected override void Fetch(MetadataResult<Movie> result, string path, CancellationToken cancellationToken)
        {
            new MovieXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return GetXmlFileInfo(info, FileSystem);
        }

        public static FileSystemMetadata GetXmlFileInfo(ItemInfo info, IFileSystem fileSystem)
        {
            var fileInfo = fileSystem.GetFileSystemInfo(info.Path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : fileSystem.GetDirectoryInfo(fileSystem.GetDirectoryName(info.Path));

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, fileSystem.GetFileNameWithoutExtension(info.Path) + ".xml");

            var file = fileSystem.GetFileInfo(specificFile);

            // In a mixed folder, only {moviename}.xml is supported
            if (info.IsInMixedFolder)
            {
                return file;
            }

            // If in it's own folder, prefer movie.xml, but allow the specific file as well
            var movieFile = fileSystem.GetFileInfo(Path.Combine(directoryPath, "movie.xml"));

            return movieFile.Exists ? movieFile : file;
        }
    }
}
