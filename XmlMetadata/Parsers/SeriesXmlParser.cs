using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

using System;
using System.Xml;
using System.Threading.Tasks;

namespace XmlMetadata.Parsers
{
    /// <summary>
    /// Class SeriesXmlParser
    /// </summary>
    public class SeriesXmlParser : BaseItemXmlParser<Series>
    {
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<Series> item)
        {
            switch (reader.Name)
            {
                case "Series":
                    //MB generated metadata is within a "Series" node
                    using (var subTree = reader.ReadSubtree())
                    {
                        await subTree.MoveToContentAsync().ConfigureAwait(false);

                        // Loop through each element
                        while (subTree.Read())
                        {
                            if (subTree.NodeType == XmlNodeType.Element)
                            {
                                await FetchDataFromXmlNode(subTree, item).ConfigureAwait(false);
                            }
                        }

                    }
                    break;

                case "id":
                    string id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.Item.SetProviderId(MetadataProviders.Tvdb, id);
                    }
                    break;

                case "DisplayOrder":

                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (Enum.TryParse(val, true, out SeriesDisplayOrder seriesDisplayOrder))
                        {
                            item.Item.DisplayOrder = seriesDisplayOrder;
                        }
                    }
                    break;

                case "Status":
                    {
                        var status = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            SeriesStatus seriesStatus;
                            if (Enum.TryParse(status, true, out seriesStatus))
                            {
                                item.Item.Status = seriesStatus;
                            }
                            else
                            {
                                Logger.Info("Unrecognized series status: " + status);
                            }
                        }

                        break;
                    }

                default:
                    await base.FetchDataFromXmlNode(reader, item).ConfigureAwait(false);
                    break;
            }
        }

        public SeriesXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }
}
