using System.IO;
using System.Threading;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

using XmlMetadata.Parsers;
using System;
using MediaBrowser.Controller.Entities;
using System.Threading.Tasks;

namespace XmlMetadata.Providers
{
    public class PersonXmlProvider : BaseXmlProvider<Person>
    {
        private readonly IProviderManager _providerManager;
        

        public PersonXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            
        }

        protected override Task Fetch(MetadataResult<Person> result, string path, CancellationToken cancellationToken)
        {
            return new BaseItemXmlParser<Person>(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var path = info.Path;

            if (string.IsNullOrEmpty(path) || BaseItem.MediaSourceManager.GetPathProtocol(path.AsSpan()) != MediaBrowser.Model.MediaInfo.MediaProtocol.File)
            {
                return null;
            }

            return directoryService.GetFile(Path.Combine(path, "person.xml"));
        }
    }
}
