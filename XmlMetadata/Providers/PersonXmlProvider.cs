using System.IO;
using System.Threading;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

using XmlMetadata.Parsers;

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

        protected override void Fetch(MetadataResult<Person> result, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Person>(Logger, _providerManager, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "person.xml"));
        }
    }
}
