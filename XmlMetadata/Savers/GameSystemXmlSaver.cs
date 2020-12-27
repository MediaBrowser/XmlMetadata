using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace XmlMetadata
{
    public class GameSystemXmlSaver : BaseXmlSaver
    {
        public GameSystemXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger) 
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.IsFileProtocol)
            {
                return false;
            }

            return item is GameSystem && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
        }

        protected override string GetLocalSavePath(BaseItem item)
        {
            return Path.Combine(item.Path, "gamesystem.xml");
        }

        protected override string GetRootElementName(BaseItem item)
        {
            return "Item";
        }
    }
}
