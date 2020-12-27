using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities.Audio;
using System;
using System.Xml;
using System.Threading.Tasks;

namespace XmlMetadata.Parsers
{
    public class MusicVideoXmlParser : BaseVideoXmlParser<MusicVideo>
    {
        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<MusicVideo> item)
        {
            switch (reader.Name)
            {
                case "Artist":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Item.SetArtists(val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        }

                        break;
                    }

                case "Album":
                    item.Item.Album = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    break;

                default:
                    await base.FetchDataFromXmlNode(reader, item).ConfigureAwait(false);
                    break;
            }
        }


        public MusicVideoXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem) : base(logger, providerManager, fileSystem)
        {
        }
    }
}
