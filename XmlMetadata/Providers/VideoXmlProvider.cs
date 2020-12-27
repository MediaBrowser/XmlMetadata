using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

using System.Threading;
using XmlMetadata.Parsers;
using System.Threading.Tasks;

namespace XmlMetadata.Providers
{
    class VideoXmlProvider : BaseXmlProvider<Video>
    {
        private readonly IProviderManager _providerManager;
        

        public VideoXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            
        }

        protected override Task Fetch(MetadataResult<Video> result, string path, CancellationToken cancellationToken)
        {
            return new VideoXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
