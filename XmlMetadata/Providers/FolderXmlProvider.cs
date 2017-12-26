using System.IO;
using System.Threading;

using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;
using XmlMetadata.Parsers;

namespace XmlMetadata.Providers
{
    /// <summary>
    /// Provides metadata for Folders and all subclasses by parsing folder.xml
    /// </summary>
    public class FolderXmlProvider : BaseXmlProvider<Folder>
    {
        private readonly IProviderManager _providerManager;
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

        public FolderXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
            : base(fileSystem, logger)
        {
            _providerManager = providerManager;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        protected override void Fetch(MetadataResult<Folder> result, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Folder>(Logger, _providerManager, XmlReaderSettingsFactory, FileSystem).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "folder.xml"));
        }
    }
}
