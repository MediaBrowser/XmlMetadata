using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

using System.IO;
using System.Threading;
using XmlMetadata.Parsers;
using System.Threading.Tasks;

namespace XmlMetadata.Providers
{
    /// <summary>
    /// Class SeriesProviderFromXml
    /// </summary>
    public class SeriesXmlProvider : BaseXmlProvider<Series>
    {
        private readonly IProviderManager _providerManager;
        

        public SeriesXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            
        }

        protected override Task Fetch(MetadataResult<Series> result, string path, CancellationToken cancellationToken)
        {
            return new SeriesXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "series.xml"));
        }
    }
}
