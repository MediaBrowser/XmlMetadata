using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;

namespace XmlMetadata.Providers
{
    public abstract class BaseXmlProvider<T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor, IHasOrder
        where T : BaseItem, new()
    {
        protected IFileSystem FileSystem;

        protected ILogger Logger;

        public async Task<MetadataResult<T>> GetMetadata(ItemInfo info,
            LibraryOptions libraryOptions,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            var file = GetXmlFile(info, directoryService);

            if (file == null)
            {
                return result;
            }

            var path = file.FullName;

            Logger.Debug("{0} will fetch xml from {1}", GetType().Name, path);

            try
            {
                result.Item = new T();

                await Fetch(result, path, cancellationToken).ConfigureAwait(false);
                result.HasMetadata = true;
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }
            catch (IOException)
            {
                result.HasMetadata = false;
            }

            return result;
        }

        protected abstract Task Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken);

        protected BaseXmlProvider(IFileSystem fileSystem, ILogger logger)
        {
            FileSystem = fileSystem;
            Logger = logger;
        }

        protected abstract FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService);

        public bool HasChanged(BaseItem item, LibraryOptions libraryOptions, IDirectoryService directoryService)
        {
            var file = GetXmlFile(new ItemInfo(item), directoryService);

            if (file == null)
            {
                return false;
            }

            return file.Exists && item.IsGreaterThanDateLastSaved(FileSystem.GetLastWriteTimeUtc(file));
        }

        public string Name
        {
            get
            {
                return XmlProviderUtils.Name;
            }
        }

        public virtual int Order
        {
            get
            {
                // After Nfo
                return 1;
            }
        }
    }

    static class XmlProviderUtils
    {
        public static string Name
        {
            get
            {
                return "Emby Xml";
            }
        }
    }
}
