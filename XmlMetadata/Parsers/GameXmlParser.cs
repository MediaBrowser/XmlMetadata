using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace XmlMetadata.Parsers
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class GameXmlParser : BaseItemXmlParser<Game>
    {
        public Task FetchAsync(MetadataResult<Game> item, string metadataFile, CancellationToken cancellationToken)
        {
            return Fetch(item, metadataFile, cancellationToken);
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="result">The result.</param>
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<Game> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "GamesDbId":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.SetProviderId(MetadataProviders.Gamesdb, val);
                        }
                        break;
                    }


                default:
                    await base.FetchDataFromXmlNode(reader, result).ConfigureAwait(false);
                    break;
            }
        }

        public GameXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }
}
