using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace XmlMetadata
{
    /// <summary>
    /// Saves game.xml for games
    /// </summary>
    public class GameXmlSaver : BaseXmlSaver
    {
        public GameXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.IsFileProtocol)
            {
                return false;
            }

            return item is Game && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
        }

        protected override string GetLocalSavePath(BaseItem item)
        {
            return GetGameSavePath((Game)item);
        }

        protected override string GetRootElementName(BaseItem item)
        {
            return "Item";
        }

        public static string GetGameSavePath(Game item)
        {
            return Path.ChangeExtension(item.Path, ".xml");
        }
    }
}
