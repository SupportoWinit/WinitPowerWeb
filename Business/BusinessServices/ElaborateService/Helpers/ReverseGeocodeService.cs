using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BingMapsRESTToolkit;

public class Indirizzo
{
    public string Via { get; set; }
    public string Citta { get; set; }
    public string Provincia { get; set; }
    public string CAP { get; set; }
    public string Regione { get; set; }
    public string Paese { get; set; }
}

public class Location
{
    public double Latitudine { get; set; }
    public double Longitudine { get; set; }
}

internal class AddressSearchInfo
{
    public string Original { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string Postcode { get; set; }
}

public class ReverseGeocodeService
{
    private static readonly HttpClient Client = new HttpClient();
    private const int MinimumGeocodeConfidence = 6;

    private readonly string _apiKey;

    public ReverseGeocodeService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<Indirizzo> OttieniIndirizzoAsync(double lat, double lon)
    {
        var url = $"https://api.opencagedata.com/geocode/v1/json?q={lat}+{lon}&key={_apiKey}&language=it&no_annotations=1";

        var response = await Client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return null;

        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var components = results[0].GetProperty("components");

        string GetProp(params string[] names)
        {
            foreach (var name in names)
            {
                if (components.TryGetProperty(name, out var value))
                    return value.GetString();
            }
            return "";
        }

        return new Indirizzo
        {
            Via = GetProp("road", "footway", "path"),
            Citta = GetProp("city", "town", "village"),
            Provincia = GetProp("state_district", "county"),
            CAP = GetProp("postcode"),
            Regione = GetProp("state"),
            Paese = GetProp("country")
        };
    }

    public async Task<Location> OttieniLocationDaIndirizzoAsync(string indirizzo)
    {
        var addressInfo = ParseAddress(indirizzo);

        foreach (var query in BuildAddressQueries(addressInfo))
        {
            var location = await TryGetLocationFromOpenCageAsync(query, addressInfo);

            if (location != null)
                return location;
        }

        foreach (var query in BuildAddressQueries(addressInfo))
        {
            var location = await TryGetLocationFromPhotonAsync(query, addressInfo);

            if (location != null)
                return location;
        }

        return null;
    }

    private async Task<Location> TryGetLocationFromOpenCageAsync(string indirizzo, AddressSearchInfo addressInfo)
    {
        var url = $"https://api.opencagedata.com/geocode/v1/json?q={Uri.EscapeDataString(indirizzo)}&key={_apiKey}&language=it&limit=5&countrycode=it&no_annotations=1";
        var response = await Client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return null;

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        JsonElement? bestResult = null;
        var bestScore = -1;

        foreach (var result in results.EnumerateArray())
        {
            if (IsIncompatibleResult(result, addressInfo))
                continue;

            var confidence = GetConfidence(result);
            var score = confidence + GetComponentScore(result) + GetAddressMatchScore(result, addressInfo);

            if (score > bestScore)
            {
                bestScore = score;
                bestResult = result;
            }
        }

        if (!bestResult.HasValue || GetConfidence(bestResult.Value) < MinimumGeocodeConfidence)
            return null;

        var geometry = bestResult.Value.GetProperty("geometry");

        return new Location
        {
            Latitudine = geometry.GetProperty("lat").GetDouble(),
            Longitudine = geometry.GetProperty("lng").GetDouble()
        };
    }

    private async Task<Location> TryGetLocationFromPhotonAsync(string indirizzo, AddressSearchInfo addressInfo)
    {
        var url = $"https://photon.komoot.io/api/?limit=5&q={Uri.EscapeDataString(indirizzo)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("PowerWeb/1.0");

        var response = await Client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return null;

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            return null;

        JsonElement? bestFeature = null;
        var bestScore = -1;

        foreach (var feature in features.EnumerateArray())
        {
            if (IsIncompatiblePhotonResult(feature, addressInfo))
                continue;

            var score = GetPhotonMatchScore(feature, addressInfo);
            if (score > bestScore)
            {
                bestScore = score;
                bestFeature = feature;
            }
        }

        if (!bestFeature.HasValue || bestScore < 6)
            return null;

        var coordinates = bestFeature.Value.GetProperty("geometry").GetProperty("coordinates");

        return new Location
        {
            Longitudine = coordinates[0].GetDouble(),
            Latitudine = coordinates[1].GetDouble()
        };
    }

    private static AddressSearchInfo ParseAddress(string indirizzo)
    {
        if (String.IsNullOrWhiteSpace(indirizzo))
            return new AddressSearchInfo();

        var parts = indirizzo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).ToArray();
        var capMatch = Regex.Match(indirizzo, @"\b\d{5}\b");
        var street = parts.Length > 0 ? parts[0] : indirizzo;
        var houseNumber = GetHouseNumberAfterPostcode(parts, capMatch.Success ? capMatch.Value : String.Empty);

        if (!String.IsNullOrWhiteSpace(houseNumber) && !Regex.IsMatch(street, @"\b" + Regex.Escape(houseNumber) + @"\b"))
            street = String.Format("{0} {1}", street, houseNumber);

        var info = new AddressSearchInfo
        {
            Original = indirizzo,
            Street = street,
            City = GetCityFromAddressParts(parts, capMatch.Success ? capMatch.Value : String.Empty),
            Postcode = capMatch.Success ? capMatch.Value : String.Empty
        };

        return info;
    }

    private static IEnumerable<string> BuildAddressQueries(AddressSearchInfo addressInfo)
    {
        if (addressInfo == null || String.IsNullOrWhiteSpace(addressInfo.Original))
            yield break;

        var normalized = NormalizeAddress(addressInfo.Original);
        var expanded = ExpandItalianStreetAbbreviations(normalized);
        var expandedWithoutRepeatedLetters = NormalizeRepeatedLetters(expanded);
        var withoutStreetWords = RemoveStreetWords(expanded);
        var expandedStreet = ExpandItalianStreetAbbreviations(NormalizeAddress(addressInfo.Street));
        var expandedStreetWithoutRepeatedLetters = NormalizeRepeatedLetters(expandedStreet);
        var streetWithoutArticles = RemoveItalianArticles(expandedStreet);
        var streetWithoutInitials = RemoveSingleLetterInitials(streetWithoutArticles);
        var streetWithoutGivenNames = RemoveCommonItalianGivenNames(streetWithoutInitials);
        var streetWithoutHouseNumber = RemoveHouseNumbers(streetWithoutGivenNames);
        var streetWithoutRepeatedLetters = NormalizeRepeatedLetters(streetWithoutGivenNames);
        var streetWithoutRepeatedLettersAndHouseNumber = RemoveHouseNumbers(streetWithoutRepeatedLetters);
        var cityAndPostcode = String.Join(" ", new[] { addressInfo.Postcode, addressInfo.City }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructured = String.Join(", ", new[] { expandedStreet, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructuredWithoutRepeatedLetters = String.Join(", ", new[] { expandedStreetWithoutRepeatedLetters, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructuredWithoutArticles = String.Join(", ", new[] { streetWithoutArticles, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructuredWithoutInitials = String.Join(", ", new[] { streetWithoutInitials, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructuredWithoutGivenNames = String.Join(", ", new[] { streetWithoutGivenNames, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructuredWithoutHouseNumber = String.Join(", ", new[] { streetWithoutHouseNumber, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedStructuredWithoutRepeatedLettersAndHouseNumber = String.Join(", ", new[] { streetWithoutRepeatedLettersAndHouseNumber, cityAndPostcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var postcodeOnlyStructured = String.Join(", ", new[] { expandedStreet, addressInfo.Postcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var postcodeOnlyStructuredWithoutHouseNumber = String.Join(", ", new[] { streetWithoutHouseNumber, addressInfo.Postcode, "Italia" }.Where(value => !String.IsNullOrWhiteSpace(value)));
        var expandedWithoutArticles = RemoveItalianArticles(expanded);
        var expandedWithoutInitials = RemoveSingleLetterInitials(expandedWithoutArticles);
        var expandedWithoutGivenNames = RemoveCommonItalianGivenNames(expandedWithoutInitials);
        var expandedWithoutRepeatedGivenNames = NormalizeRepeatedLetters(expandedWithoutGivenNames);
        var streetOnlyWithoutArticles = RemoveItalianArticles(withoutStreetWords);
        var streetOnlyWithoutInitials = RemoveSingleLetterInitials(streetOnlyWithoutArticles);
        var streetOnlyWithoutGivenNames = RemoveCommonItalianGivenNames(streetOnlyWithoutInitials);
        var streetOnlyWithoutRepeatedGivenNames = NormalizeRepeatedLetters(streetOnlyWithoutGivenNames);

        foreach (var query in new[]
        {
            expandedStructured,
            expandedStructuredWithoutRepeatedLetters,
            expandedStructuredWithoutArticles,
            expandedStructuredWithoutInitials,
            expandedStructuredWithoutGivenNames,
            expandedStructuredWithoutHouseNumber,
            expandedStructuredWithoutRepeatedLettersAndHouseNumber,
            postcodeOnlyStructured,
            postcodeOnlyStructuredWithoutHouseNumber,
            addressInfo.Original,
            normalized,
            expanded,
            expandedWithoutRepeatedLetters,
            expandedWithoutArticles,
            expandedWithoutInitials,
            expandedWithoutGivenNames,
            expandedWithoutRepeatedGivenNames,
            RemoveHouseNumbers(expandedWithoutGivenNames),
            RemoveHouseNumbers(expandedWithoutRepeatedGivenNames),
            withoutStreetWords,
            streetOnlyWithoutArticles,
            streetOnlyWithoutInitials,
            streetOnlyWithoutGivenNames,
            streetOnlyWithoutRepeatedGivenNames,
            RemoveHouseNumbers(streetOnlyWithoutGivenNames),
            RemoveHouseNumbers(streetOnlyWithoutRepeatedGivenNames)
        }.Where(q => !String.IsNullOrWhiteSpace(q)).Distinct())
            yield return query;
    }

    private static string GetHouseNumberAfterPostcode(string[] parts, string postcode)
    {
        if (parts == null || String.IsNullOrWhiteSpace(postcode))
            return String.Empty;

        foreach (var part in parts.Skip(1))
        {
            var match = Regex.Match(part, @"\b" + Regex.Escape(postcode) + @"\s+(\d+[a-zA-Z]?(?:\s*[-/]\s*(?:\d+[a-zA-Z]?|[a-zA-Z]))?)\b");
            if (match.Success)
                return match.Groups[1].Value;
        }

        return String.Empty;
    }

    private static string GetCityFromAddressParts(string[] parts, string postcode)
    {
        if (parts == null || parts.Length < 2)
            return String.Empty;

        foreach (var part in parts.Skip(1))
        {
            var normalizedPart = NormalizeAddress(part);

            if (IsCountryName(normalizedPart))
                continue;

            if (!String.IsNullOrWhiteSpace(postcode) && normalizedPart.Contains(postcode))
            {
                var city = Regex.Replace(part, @"\b" + Regex.Escape(postcode) + @"\b", String.Empty).Trim();
                if (!String.IsNullOrWhiteSpace(city) && !IsHouseNumber(city))
                    return city;

                continue;
            }

            if (!Regex.IsMatch(part, @"^\d{5}$"))
                return part;
        }

        return String.Empty;
    }

    private static string NormalizeAddress(string address)
    {
        var normalized = address.ToLowerInvariant();
        normalized = normalized.Replace(",", " ");
        normalized = normalized.Replace(".", " ");
        normalized = normalized.Replace("/", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    private static string ExpandItalianStreetAbbreviations(string address)
    {
        var replacements = new Dictionary<string, string>
        {
            { @"\bv\b", "via" },
            { @"\bvle\b", "viale" },
            { @"\bcso\b", "corso" },
            { @"\bpza\b", "piazza" },
            { @"\bpzza\b", "piazza" },
            { @"\blgo\b", "largo" },
            { @"\bfraz\b", "frazione" },
            { @"\bloc\b", "localita" },
            { @"\bsnc\b", "senza numero civico" }
        };

        var expanded = address;
        foreach (var replacement in replacements)
            expanded = Regex.Replace(expanded, replacement.Key, replacement.Value, RegexOptions.IgnoreCase);

        expanded = Regex.Replace(expanded, @"\bs\s+maria\b", "santa maria", RegexOptions.IgnoreCase);
        expanded = Regex.Replace(expanded, @"\bs\s+([aeiou]\w*)", "sant'$1", RegexOptions.IgnoreCase);
        expanded = Regex.Replace(expanded, @"\bs\s+([bcdfghlmnpqrstvwxyz]\w*)", "san $1", RegexOptions.IgnoreCase);

        return Regex.Replace(expanded, @"\s+", " ").Trim();
    }

    private static string RemoveStreetWords(string address)
    {
        var value = Regex.Replace(address, @"\b(via|viale|corso|piazza|piazzale|largo|localita|frazione)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string RemoveItalianArticles(string address)
    {
        var value = Regex.Replace(address, @"\b(del|dello|della|dei|degli|delle|di|da|dal|dalla|dai|dagli|alle|alla|al|ai|agli|il|lo|la|i|gli|le)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string RemoveSingleLetterInitials(string address)
    {
        var value = Regex.Replace(address, @"\b[a-zA-Z]\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string RemoveCommonItalianGivenNames(string address)
    {
        var value = Regex.Replace(address, @"\b(antonio|angelo|anna|aldo|andrea|carlo|cesare|dante|enrico|francesco|franco|giacomo|giorgio|giovanni|giuseppe|guglielmo|leonardo|luigi|maria|marco|mario|paolo|pietro|roberto|stefano|vittorio)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string RemoveHouseNumbers(string address)
    {
        var value = Regex.Replace(address, @"\b\d+[a-zA-Z]?(?:\s*[-/]\s*(?:\d+[a-zA-Z]?|[a-zA-Z]))?\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static bool IsHouseNumber(string value)
    {
        return !String.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), @"^\d+[a-zA-Z]?(?:\s*[-/]\s*(?:\d+[a-zA-Z]?|[a-zA-Z]))?$");
    }

    private static string NormalizeRepeatedLetters(string address)
    {
        return String.IsNullOrWhiteSpace(address) ? String.Empty : Regex.Replace(address, @"([a-zA-Z])\1{2,}", "$1$1", RegexOptions.IgnoreCase);
    }

    private static bool IsCountryName(string value)
    {
        return value == "italia" || value == "italy";
    }

    private static int GetConfidence(JsonElement result)
    {
        if (result.TryGetProperty("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number)
            return confidence.GetInt32();

        return 0;
    }

    private static int GetComponentScore(JsonElement result)
    {
        if (!result.TryGetProperty("components", out var components))
            return 0;

        var score = 0;

        if (HasAnyComponent(components, "road", "pedestrian", "footway", "path"))
            score += 2;
        if (HasAnyComponent(components, "house_number"))
            score += 2;
        if (HasAnyComponent(components, "postcode"))
            score += 1;
        if (HasAnyComponent(components, "city", "town", "village", "municipality"))
            score += 1;

        return score;
    }

    private static bool IsIncompatibleResult(JsonElement result, AddressSearchInfo addressInfo)
    {
        if (addressInfo == null || !result.TryGetProperty("components", out var components))
            return false;

        var resultPostcode = GetComponentValue(components, "postcode");
        if (!String.IsNullOrWhiteSpace(addressInfo.Postcode) && !String.IsNullOrWhiteSpace(resultPostcode) && resultPostcode != addressInfo.Postcode && !HasNearbyPostcodeAndMatchingStreet(components, addressInfo, resultPostcode))
            return true;

        var resultCity = GetComponentValue(components, "city", "town", "village", "municipality");
        if (!String.IsNullOrWhiteSpace(addressInfo.City) && !String.IsNullOrWhiteSpace(resultCity) && NormalizeAddress(resultCity) != NormalizeAddress(addressInfo.City) && !HasNearbyPostcodeAndMatchingStreet(components, addressInfo, resultPostcode))
            return true;

        if (HasStreetInRequest(addressInfo) && !HasAnyComponent(components, "road", "pedestrian", "footway", "path"))
            return true;

        if (!String.IsNullOrWhiteSpace(addressInfo.City) && String.IsNullOrWhiteSpace(resultCity) && !HasAnyComponent(components, "road", "pedestrian", "footway", "path"))
            return true;

        return false;
    }

    private static int GetAddressMatchScore(JsonElement result, AddressSearchInfo addressInfo)
    {
        if (addressInfo == null || !result.TryGetProperty("components", out var components))
            return 0;

        var score = 0;
        var resultPostcode = GetComponentValue(components, "postcode");
        var resultCity = GetComponentValue(components, "city", "town", "village", "municipality");

        if (!String.IsNullOrWhiteSpace(addressInfo.Postcode))
            score += resultPostcode == addressInfo.Postcode ? 8 : HasNearbyPostcodeAndMatchingStreet(components, addressInfo, resultPostcode) ? 2 : -8;

        if (!String.IsNullOrWhiteSpace(addressInfo.City))
            score += NormalizeAddress(resultCity) == NormalizeAddress(addressInfo.City) ? 8 : -8;

        return score;
    }

    private static bool HasAnyComponent(JsonElement components, params string[] names)
    {
        foreach (var name in names)
            if (components.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null && !String.IsNullOrWhiteSpace(value.GetString()))
                return true;

        return false;
    }

    private static bool HasStreetInRequest(AddressSearchInfo addressInfo)
    {
        if (addressInfo == null || String.IsNullOrWhiteSpace(addressInfo.Street))
            return false;

        var street = RemoveStreetWords(RemoveItalianArticles(ExpandItalianStreetAbbreviations(NormalizeAddress(addressInfo.Street))));
        return !String.IsNullOrWhiteSpace(street);
    }

    private static bool HasNearbyPostcodeAndMatchingStreet(JsonElement components, AddressSearchInfo addressInfo, string resultPostcode)
    {
        return !String.IsNullOrWhiteSpace(resultPostcode)
               && HaveSamePostcodeArea(addressInfo.Postcode, resultPostcode)
               && HasRequestedStreetNameInComponents(components, addressInfo);
    }

    private static bool HaveSamePostcodeArea(string requestedPostcode, string resultPostcode)
    {
        return !String.IsNullOrWhiteSpace(requestedPostcode)
               && !String.IsNullOrWhiteSpace(resultPostcode)
               && requestedPostcode.Length >= 3
               && resultPostcode.Length >= 3
               && requestedPostcode.Substring(0, 3) == resultPostcode.Substring(0, 3);
    }

    private static bool HasRequestedStreetNameInComponents(JsonElement components, AddressSearchInfo addressInfo)
    {
        var requestedStreet = NormalizeRequestedStreet(addressInfo);
        if (String.IsNullOrWhiteSpace(requestedStreet))
            return false;

        var resultStreet = NormalizeAddress(GetComponentValue(components, "road", "pedestrian", "footway", "path"));
        return ContainsRequestedStreetTokens(resultStreet, requestedStreet);
    }

    private static string GetComponentValue(JsonElement components, params string[] names)
    {
        foreach (var name in names)
            if (components.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null)
                return value.GetString();

        return String.Empty;
    }

    private static bool IsIncompatiblePhotonResult(JsonElement feature, AddressSearchInfo addressInfo)
    {
        if (addressInfo == null || !feature.TryGetProperty("properties", out var properties))
            return true;

        var postcode = GetPhotonProperty(properties, "postcode");
        if (!String.IsNullOrWhiteSpace(addressInfo.Postcode) && !String.IsNullOrWhiteSpace(postcode) && postcode != addressInfo.Postcode)
            return true;

        var city = GetPhotonProperty(properties, "city");
        if (!String.IsNullOrWhiteSpace(addressInfo.City) && !String.IsNullOrWhiteSpace(city) && NormalizeAddress(city) != NormalizeAddress(addressInfo.City))
            return true;

        return !HasRequestedStreetName(properties, addressInfo);
    }

    private static int GetPhotonMatchScore(JsonElement feature, AddressSearchInfo addressInfo)
    {
        var properties = feature.GetProperty("properties");
        var score = 0;

        if (GetPhotonProperty(properties, "postcode") == addressInfo.Postcode)
            score += 3;

        if (!String.IsNullOrWhiteSpace(addressInfo.City) && NormalizeAddress(GetPhotonProperty(properties, "city")) == NormalizeAddress(addressInfo.City))
            score += 3;

        if (HasRequestedStreetName(properties, addressInfo))
            score += 6;

        return score;
    }

    private static bool HasRequestedStreetName(JsonElement properties, AddressSearchInfo addressInfo)
    {
        var requestedStreet = NormalizeRequestedStreet(addressInfo);
        if (String.IsNullOrWhiteSpace(requestedStreet))
            return false;

        var resultStreet = NormalizeAddress(String.Join(" ", new[] { GetPhotonProperty(properties, "name"), GetPhotonProperty(properties, "street") }.Where(value => !String.IsNullOrWhiteSpace(value))));
        return ContainsRequestedStreetTokens(resultStreet, requestedStreet);
    }

    private static string NormalizeRequestedStreet(AddressSearchInfo addressInfo)
    {
        return addressInfo == null ? String.Empty : NormalizeRepeatedLetters(RemoveHouseNumbers(RemoveStreetWords(RemoveItalianArticles(ExpandItalianStreetAbbreviations(NormalizeAddress(addressInfo.Street))))));
    }

    private static bool ContainsRequestedStreetTokens(string resultStreet, string requestedStreet)
    {
        var requestedTokens = requestedStreet.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(token => token.Length > 2).ToArray();

        return requestedTokens.Any() && requestedTokens.All(token => resultStreet.Contains(token));
    }

    private static string GetPhotonProperty(JsonElement properties, string name)
    {
        if (properties.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null)
            return value.GetString();

        return String.Empty;
    }

}
