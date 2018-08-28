using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace XmlMetadata.Providers
{
    public abstract class BaseXmlProvider<T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor, IHasOrder
        where T : BaseItem, new()
    {
        protected IFileSystem FileSystem;

        protected ILogger Logger;

        public Task<MetadataResult<T>> GetMetadata(ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            var file = GetXmlFile(info, directoryService);

            if (file == null)
            {
                return Task.FromResult(result);
            }

            var path = file.FullName;

            Logger.Debug("{0} will fetch xml from {1}", GetType().Name, path);

            try
            {
                result.Item = new T();

                Fetch(result, path, cancellationToken);
                result.HasMetadata = true;
            }
            catch (FileNotFoundException e)
            {
                Logger.ErrorException("Error parsing {0}", e, path);
                result.HasMetadata = false;
            }
            catch (IOException e)
            {
                Logger.ErrorException("Error parsing {0}", e, path);
                result.HasMetadata = false;
            }

            return Task.FromResult(result);
        }

        protected abstract void Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken);

        protected BaseXmlProvider(IFileSystem fileSystem, ILogger logger)
        {
            FileSystem = fileSystem;
            Logger = logger;
        }

        protected abstract FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService);

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
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

        public  virtual int Order
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
