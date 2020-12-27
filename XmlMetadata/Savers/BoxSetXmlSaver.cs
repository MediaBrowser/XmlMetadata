using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Common.Configuration;

namespace XmlMetadata
{
    public class BoxSetXmlSaver : BaseXmlSaver
    {
        public BoxSetXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger) : 
            base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            return item is BoxSet && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var collection = (BoxSet)item;

            writer.WriteElementString("DisplayOrder", collection.DisplayOrder.ToString());
        }

        protected override string GetLocalSavePath(BaseItem item)
        {
            return Path.Combine(item.GetInternalMetadataPath(), "collection.xml");
        }
    }
}
