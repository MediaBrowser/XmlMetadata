using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Dto;
using System.Xml;
using System.Threading.Tasks;

namespace XmlMetadata.Parsers
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class BaseVideoXmlParser<T> : BaseItemXmlParser<T>
        where T : Video
    {
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> item)
        {
            switch (reader.Name)
            {
                case "TmdbCollectionName":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Item.SetCollections(new[] { val });
                        }

                        break;
                    }

                default:
                    await base.FetchDataFromXmlNode(reader, item).ConfigureAwait(false);
                    break;
            }
        }

        public BaseVideoXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }

    public class MovieXmlParser : BaseVideoXmlParser<Movie>
    {
        public MovieXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }

    public class VideoXmlParser : BaseVideoXmlParser<Video>
    {
        public VideoXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }
}
