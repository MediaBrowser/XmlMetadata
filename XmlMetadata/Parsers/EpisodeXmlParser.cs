using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Threading.Tasks;

namespace XmlMetadata.Parsers
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class EpisodeXmlParser : BaseItemXmlParser<Episode>
    {
        private List<LocalImageInfo> _imagesFound;
        private readonly IFileSystem _fileSystem;

        public EpisodeXmlParser(ILogger logger, IFileSystem fileSystem, IProviderManager providerManager)
            : base(logger, providerManager, fileSystem)
        {
            _fileSystem = fileSystem;
        }

        private string _xmlPath;

        public Task Fetch(MetadataResult<Episode> item,
            List<LocalImageInfo> images,
            string metadataFile,
            CancellationToken cancellationToken)
        {
            _imagesFound = images;
            _xmlPath = metadataFile;

            return Fetch(item, metadataFile, cancellationToken);
        }

        protected override async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<Episode> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "Episode":

                    //MB generated metadata is within an "Episode" node
                    using (var subTree = reader.ReadSubtree())
                    {
                        await subTree.MoveToContentAsync().ConfigureAwait(false);

                        // Loop through each element
                        while (subTree.Read())
                        {
                            if (subTree.NodeType == XmlNodeType.Element)
                            {
                                await FetchDataFromXmlNode(subTree, result).ConfigureAwait(false);
                            }
                        }

                    }
                    break;

                case "filename":
                    {
                        var filename = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            // Strip off everything but the filename. Some metadata tools like MetaBrowser v1.0 will have an 'episodes' prefix
                            // even though it's actually using the metadata folder.
                            filename = Path.GetFileName(filename);

                            var parentFolder = _fileSystem.GetDirectoryName(_xmlPath);
                            filename = Path.Combine(parentFolder, filename);
                            var file = _fileSystem.GetFileInfo(filename);

                            if (file.Exists)
                            {
                                _imagesFound.Add(new LocalImageInfo
                                {
                                    Type = ImageType.Primary,
                                    FileInfo = file
                                });
                            }
                        }
                        break;
                    }
                case "SeasonNumber":
                    {
                        var number = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.ParentIndexNumber = num;
                            }
                        }
                        break;
                    }

                case "EpisodeNumber":
                    {
                        var number = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.IndexNumber = num;
                            }
                        }
                        break;
                    }

                case "EpisodeNumberEnd":
                    {
                        var number = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.IndexNumberEnd = num;
                            }
                        }
                        break;
                    }

                case "airsbefore_episode":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out rval))
                            {
                                item.AirsBeforeEpisodeNumber = rval;
                            }
                        }

                        break;
                    }

                case "airsafter_season":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out rval))
                            {
                                item.AirsAfterSeasonNumber = rval;
                            }
                        }

                        break;
                    }

                case "airsbefore_season":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out rval))
                            {
                                item.AirsBeforeSeasonNumber = rval;
                            }
                        }

                        break;
                    }

                case "EpisodeName":
                    {
                        var name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            item.Name = name;
                        }
                        break;
                    }

                default:
                    await base.FetchDataFromXmlNode(reader, result).ConfigureAwait(false);
                    break;
            }
        }
    }
}
