using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System.Threading.Tasks;

namespace XmlMetadata.Parsers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseItemXmlParser<T>
        where T : BaseItem
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger { get; private set; }
        protected IProviderManager ProviderManager { get; private set; }

        private Dictionary<string, string> _validProviderIds;

        protected IFileSystem FileSystem { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemXmlParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BaseItemXmlParser(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem)
        {
            Logger = logger;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
        }

        private XmlReaderSettings Create(bool enableValidation)
        {
            var settings = new XmlReaderSettings();

            if (!enableValidation)
            {
                settings.ValidationType = ValidationType.None;
            }

            return settings;
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Task Fetch(MetadataResult<T> item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentNullException();
            }

            var settings = Create(false);

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;
            settings.Async = true;

            _validProviderIds = _validProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var idInfos = ProviderManager.GetExternalIdInfos(item.Item);

            foreach (var info in idInfos)
            {
                var id = info.Key + "Id";
                if (!_validProviderIds.ContainsKey(id))
                {
                    _validProviderIds.Add(id, info.Key);
                }
            }

            //Additional Mappings
            _validProviderIds.Add("IMDB", "Imdb");

            //Fetch(item, metadataFile, settings, Encoding.GetEncoding("ISO-8859-1"), cancellationToken);
            return Fetch(item, metadataFile, settings, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, Encoding encoding, CancellationToken cancellationToken)
        {
            item.ResetPeople();

            using (Stream fileStream = FileSystem.GetFileStream(metadataFile, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read, FileOpenOptions.SequentialScan | FileOpenOptions.Asynchronous))
            {
                using (var streamReader = new StreamReader(fileStream, encoding))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        await reader.MoveToContentAsync().ConfigureAwait(false);
                        await reader.ReadAsync().ConfigureAwait(false);

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                await FetchDataFromXmlNode(reader, item).ConfigureAwait(false);
                            }
                            else
                            {
                                await reader.ReadAsync().ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fetches metadata from one Xml Element
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The item result.</param>
        protected virtual async Task FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                // DateCreated
                case "Added":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            DateTimeOffset added;
                            if (DateTimeOffset.TryParse(val, out added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else
                            {
                                Logger.Warn("Invalid Added value found: " + val);
                            }
                        }
                        break;
                    }

                case "OriginalTitle":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(val))
                        {
                            item.OriginalTitle = val;
                        }
                        break;
                    }

                case "LocalTitle":
                    item.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    break;

                case "CriticRating":
                    {
                        var text = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(text))
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                            {
                                item.CriticRating = value;
                            }
                        }

                        break;
                    }

                case "SortTitle":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.SortName = val;
                        }
                        break;
                    }

                case "Overview":
                case "Description":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Overview = val;
                        }

                        break;
                    }

                case "Language":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        item.PreferredMetadataLanguage = val;

                        break;
                    }

                case "CountryCode":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        item.PreferredMetadataCountryCode = val;

                        break;
                    }

                case "PlaceOfBirth":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var person = item as Person;
                            if (person != null)
                            {
                                person.ProductionLocations = new string[] { val };
                            }
                        }

                        break;
                    }

                case "LockedFields":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.LockedFields = val.Split('|').Select(i =>
                            {
                                MetadataFields field;

                                if (Enum.TryParse<MetadataFields>(i, true, out field))
                                {
                                    return (MetadataFields?)field;
                                }

                                return null;

                            }).Where(i => i.HasValue).Select(i => i.Value).ToArray();
                        }

                        break;
                    }

                case "TagLines":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchFromTaglinesNode(subtree, item).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "Countries":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchFromCountriesNode(subtree, item).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "ContentRating":
                case "MPAARating":
                    {
                        var rating = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRating = rating;
                        }
                        break;
                    }

                case "CustomRating":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CustomRating = val;
                        }
                        break;
                    }

                case "RunningTime":
                    {
                        var text = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }
                        break;
                    }

                case "LockData":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.IsLocked = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }
                        break;
                    }

                case "Network":
                    {
                        foreach (var name in SplitNames(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)))
                        {
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }
                            item.AddStudio(name);
                        }
                        break;
                    }

                case "Director":
                    {
                        foreach (var p in SplitNames(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }
                case "Writer":
                    {
                        foreach (var p in SplitNames(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }

                case "Actors":
                    {

                        var actors = reader.ReadInnerXml();

                        if (actors.Contains("<"))
                        {
                            // This is one of the mis-named "Actors" full nodes created by MB2
                            // Create a reader and pass it to the persons node processor
                            using (var stringReader = new StringReader("<Persons>" + actors + "</Persons>"))
                            {
                                using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings { Async = true }))
                                {
                                    await FetchDataFromPersonsNode(xmlReader, itemResult).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            // Old-style piped string
                            foreach (var p in SplitNames(actors).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Actor }))
                            {
                                if (string.IsNullOrWhiteSpace(p.Name))
                                {
                                    continue;
                                }
                                itemResult.AddPerson(p);
                            }
                        }
                        break;
                    }

                case "GuestStars":
                    {
                        foreach (var p in SplitNames(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.GuestStar }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }

                case "Trailer":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddTrailerUrl(val);
                        }
                        break;
                    }

                case "Trailers":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchDataFromTrailersNode(subtree, item).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "ProductionYear":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int productionYear;
                            if (int.TryParse(val, out productionYear) && productionYear > 1850)
                            {
                                item.ProductionYear = productionYear;
                            }
                        }

                        break;
                    }

                case "Rating":
                case "IMDBrating":
                    {

                        var rating = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            float val;
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out val))
                            {
                                item.CommunityRating = val;
                            }
                        }
                        break;
                    }

                case "BirthDate":
                case "PremiereDate":
                case "FirstAired":
                    {
                        var firstAired = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTimeOffset airDate;

                            if (DateTimeOffset.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out airDate) && airDate.Year > 1850)
                            {
                                item.PremiereDate = airDate.ToUniversalTime();
                                item.ProductionYear = airDate.Year;
                            }
                        }

                        break;
                    }

                case "DeathDate":
                case "EndDate":
                    {
                        var firstAired = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTimeOffset airDate;

                            if (DateTimeOffset.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out airDate) && airDate.Year > 1850)
                            {
                                item.EndDate = airDate.ToUniversalTime();
                            }
                        }

                        break;
                    }

                case "Genres":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchFromGenresNode(subtree, item).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "Tags":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchFromTagsNode(subtree, item).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "Persons":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchDataFromPersonsNode(subtree, itemResult).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "Studios":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                await FetchFromStudiosNode(subtree, item).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await reader.ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    }

                case "Format3D":
                    {
                        var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                        var video = item as Video;

                        if (video != null)
                        {
                            if (string.Equals("HSBS", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.HalfSideBySide;
                            }
                            else if (string.Equals("HTAB", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                            }
                            else if (string.Equals("FTAB", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                            }
                            else if (string.Equals("FSBS", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.FullSideBySide;
                            }
                            else if (string.Equals("MVC", val, StringComparison.OrdinalIgnoreCase))
                            {
                                video.Video3DFormat = Video3DFormat.MVC;
                            }
                        }
                        break;
                    }

                default:
                    {
                        string readerName = reader.Name;
                        string providerIdValue;
                        if (_validProviderIds.TryGetValue(readerName, out providerIdValue))
                        {
                            var id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                item.SetProviderId(providerIdValue, id);
                            }
                        }
                        else
                        {
                            await reader.SkipAsync().ConfigureAwait(false);
                        }

                        break;
                    }
            }
        }

        private async Task FetchFromCountriesNode(XmlReader reader, T item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Country":
                            {
                                var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Fetches from taglines node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private async Task FetchFromTaglinesNode(XmlReader reader, T item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tagline":
                            {
                                var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.Tagline = val;
                                }
                                break;
                            }
                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Fetches from genres node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private async Task FetchFromGenresNode(XmlReader reader, T item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Genre":
                            {
                                var genre = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(genre))
                                {
                                    item.AddGenre(genre);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task FetchFromTagsNode(XmlReader reader, BaseItem item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            var tags = new List<string>();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tag":
                            {
                                var tag = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(tag))
                                {
                                    tags.Add(tag);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }

            item.SetTags(tags);
        }

        /// <summary>
        /// Fetches the data from persons node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private async Task FetchDataFromPersonsNode(XmlReader reader, MetadataResult<T> item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                        case "Actor":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    await reader.ReadAsync().ConfigureAwait(false);
                                    continue;
                                }
                                using (var subtree = reader.ReadSubtree())
                                {
                                    foreach (var person in await GetPersonsFromXmlNode(subtree).ConfigureAwait(false))
                                    {
                                        if (string.IsNullOrWhiteSpace(person.Name))
                                        {
                                            continue;
                                        }
                                        item.AddPerson(person);
                                    }
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task FetchDataFromTrailersNode(XmlReader reader, T item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Trailer":
                            {
                                var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AddTrailerUrl(val);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Fetches from studios node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private async Task FetchFromStudiosNode(XmlReader reader, T item)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Studio":
                            {
                                var studio = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(studio))
                                {
                                    item.AddStudio(studio);
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets the persons from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private async Task<PersonInfo[]> GetPersonsFromXmlNode(XmlReader reader)
        {
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;

            await reader.MoveToContentAsync().ConfigureAwait(false);
            await reader.ReadAsync().ConfigureAwait(false);

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false) ?? string.Empty;
                            break;

                        case "Type":
                            {
                                var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    PersonType personType;
                                    if (Enum.TryParse<PersonType>(val, true, out personType))
                                    {
                                        type = personType;
                                    }
                                }
                                break;
                            }

                        case "Role":
                            {
                                var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    role = val;
                                }
                                break;
                            }
                        case "SortOrder":
                            {
                                var val = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int intVal;
                                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out intVal))
                                    {
                                        sortOrder = intVal;
                                    }
                                }
                                break;
                            }

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
                else
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                }
            }

            var personInfo = new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type
            };

            return new[] { personInfo };
        }

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> SplitNames(string value)
        {
            value = value ?? string.Empty;

            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = value.IndexOf('|') == -1 && value.IndexOf(';') == -1 ? new[] { ',' } : new[] { '|', ';' };

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : Split(value, separator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Provides an additional overload for string.split
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="separators">The separators.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        private string[] Split(string val, char[] separators, StringSplitOptions options)
        {
            return val.Split(separators, options);
        }

    }
}
