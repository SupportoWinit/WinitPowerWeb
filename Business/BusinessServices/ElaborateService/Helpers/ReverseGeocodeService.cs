using System;
using System.Net.Http;
using System.Text.Json;
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

public class ReverseGeocodeService
{
    private readonly string _apiKey;

    public ReverseGeocodeService(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<Indirizzo> OttieniIndirizzoAsync(double lat, double lon)
    {
        var url = $"https://api.opencagedata.com/geocode/v1/json?q={lat}+{lon}&key={_apiKey}";

        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var results = root.GetProperty("results");
        if (results.GetArrayLength() == 0)
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
        var url = $"https://api.opencagedata.com/geocode/v1/json?q={Uri.EscapeDataString(indirizzo)}&key={_apiKey}&language=it&limit=1";

        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = root.GetProperty("results")[0];
        var geometry = result.GetProperty("geometry");
        var components = result.GetProperty("components");

        string GetProp(params string[] names)
        {
            foreach (var name in names)
            {
                if (components.TryGetProperty(name, out var value))
                    return value.GetString();
            }
            return "";
        }

        return new Location
        {
            Latitudine = geometry.GetProperty("lat").GetDouble(),
            Longitudine = geometry.GetProperty("lng").GetDouble()
        };
    }

}