using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using XmlMetadata.Parsers;
using System;
using System.Threading.Tasks;

namespace XmlMetadata.Providers
{
    public class EpisodeXmlProvider : BaseXmlProvider<Episode>
    {
        private readonly IProviderManager _providerManager;


        public EpisodeXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;

        }

        protected override async Task Fetch(MetadataResult<Episode> result, string path, CancellationToken cancellationToken)
        {
            var images = new List<LocalImageInfo>();
            var chapters = new List<ChapterInfo>();

            await new EpisodeXmlParser(Logger, FileSystem, _providerManager).Fetch(result, images, path, cancellationToken).ConfigureAwait(false);

            result.Images = images;
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var path = info.Path;

            if (string.IsNullOrEmpty(path) || BaseItem.MediaSourceManager.GetPathProtocol(path.AsSpan()) != MediaBrowser.Model.MediaInfo.MediaProtocol.File)
            {
                return null;
            }

            var metadataPath = FileSystem.GetDirectoryName(path);
            metadataPath = Path.Combine(metadataPath, "metadata");

            var metadataFile = Path.Combine(metadataPath, Path.ChangeExtension(Path.GetFileName(path), ".xml"));

            return directoryService.GetFile(metadataFile);
        }
    }
}
