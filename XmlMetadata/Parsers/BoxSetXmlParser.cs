using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Xml;
using MediaBrowser.Model.IO;
using System;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace XmlMetadata.Parsers
{
    public class BoxSetXmlParser : BaseItemXmlParser<BoxSet>
    {
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<BoxSet> item)
        {
            switch (reader.Name)
            {
                case "DisplayOrder":

                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (Enum.TryParse<CollectionDisplayOrder>(val, true, out CollectionDisplayOrder result))
                        {
                            item.Item.DisplayOrder = result;
                        }
                    }
                    break;

                default:
                    await base.FetchDataFromXmlNode(reader, item).ConfigureAwait(false);
                    break;
            }
        }

        public BoxSetXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }
}
