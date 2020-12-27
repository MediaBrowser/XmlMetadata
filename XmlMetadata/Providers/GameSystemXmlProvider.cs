using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using XmlMetadata.Parsers;
using MediaBrowser.Model.Logging;

namespace XmlMetadata.Providers
{
    public class GameSystemXmlProvider : BaseXmlProvider<GameSystem>
    {
        private readonly IProviderManager _providerManager;

        public GameSystemXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
        }

        protected override Task Fetch(MetadataResult<GameSystem> result, string path, CancellationToken cancellationToken)
        {
            return new GameSystemXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "gamesystem.xml"));
        }
    }
}
