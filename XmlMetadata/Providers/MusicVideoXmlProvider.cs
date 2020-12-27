using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

using System.Threading;
using XmlMetadata.Parsers;
using System.Threading.Tasks;

namespace XmlMetadata.Providers
{
    class MusicVideoXmlProvider : BaseXmlProvider<MusicVideo>
    {
        private readonly IProviderManager _providerManager;
        

        public MusicVideoXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            
        }

        protected override Task Fetch(MetadataResult<MusicVideo> result, string path, CancellationToken cancellationToken)
        {
            return new MusicVideoXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieXmlProvider.GetXmlFileInfo(info, FileSystem);
        }
    }
}
