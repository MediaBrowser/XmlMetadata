using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;
using System;
using System.Xml;

namespace XmlMetadata.Parsers
{
    public class MusicVideoXmlParser : BaseVideoXmlParser<MusicVideo>
    {
        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="result">The result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<MusicVideo> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "Artist":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Artists = val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        }

                        break;
                    }

                case "Album":
                    item.Album = reader.ReadElementContentAsString();
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, result);
                    break;
            }
        }

        public MusicVideoXmlParser(ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory, IFileSystem fileSystem) : base(logger, providerManager, xmlReaderSettingsFactory, fileSystem)
        {
        }
    }
}
