using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using XmlMetadata.Parsers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace XmlMetadata.Providers
{
    public class GameXmlProvider : BaseXmlProvider<Game>
    {
        private readonly IProviderManager _providerManager;

        public GameXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
        }

        protected override Task Fetch(MetadataResult<Game> result, string path, CancellationToken cancellationToken)
        {
            return new GameXmlParser(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var specificFile = Path.ChangeExtension(info.Path, ".xml");
            var file = FileSystem.GetFileInfo(specificFile);

            return info.IsInMixedFolder || file.Exists ? file : FileSystem.GetFileInfo(Path.Combine(FileSystem.GetDirectoryName(info.Path), "game.xml"));
        }
    }
}
