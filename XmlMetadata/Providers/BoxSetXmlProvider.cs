using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using XmlMetadata.Parsers;
using MediaBrowser.Common.Configuration;

namespace XmlMetadata.Providers
{
    /// <summary>
    /// Class BoxSetXmlProvider.
    /// </summary>
    public class BoxSetXmlProvider : BaseXmlProvider<BoxSet>
    {
        private readonly IProviderManager _providerManager;
        private IApplicationPaths _appPaths;

        public BoxSetXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IApplicationPaths appPaths)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            _appPaths = appPaths;
        }

        protected override Task Fetch(MetadataResult<BoxSet> result, string path, CancellationToken cancellationToken)
        {
            return new BoxSetXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        public static string GetLegacyMetadataPath(IApplicationPaths appPaths, IFileSystem fileSystem, string name)
        {
            return System.IO.Path.Combine(appPaths.DataPath, "collections", fileSystem.GetValidFilename(name) + " [boxset]");
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var file = directoryService.GetFile(Path.Combine(info.GetInternalMetadataPath(), "collection.xml"));

            if (file != null && file.Exists)
            {
                return file;
            }

            return directoryService.GetFile(Path.Combine(GetLegacyMetadataPath(_appPaths, FileSystem, info.Name), "collection.xml"));
        }
    }
}
