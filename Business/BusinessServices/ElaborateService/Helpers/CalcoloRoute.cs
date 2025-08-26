using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System;

public class CalcoloRoute
{
    public double DistanzaKm { get; set; }
    public double DurataMinuti { get; set; }
}

public class CalcoloPercorsoService
{
    private const string ApiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjJmZjdhYWQ5MTI4MzRjODlhZDViYTI3Njc2MGFiMWEwIiwiaCI6Im11cm11cjY0In0=";

    public async Task<CalcoloRoute> CalcolaPercorsoAsync(double lat1, double lon1, double lat2, double lon2)
    {
        double durata = 0;
        double distanza = 0;
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        var url = "https://api.openrouteservice.org/v2/directions/driving-car/json";

        var requestBody = new
        {
            coordinates = new List<List<double>>
            {
                new List<double> { lon1, lat1 },
                new List<double> { lon2, lat2 }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Errore: " + response.StatusCode);
            Console.WriteLine(responseString);
            return new CalcoloRoute(); // ritorna valori zero
        }

        var document = JsonDocument.Parse(responseString);
        var root = document.RootElement;

        if (lon1 != lon2 && lat1 != lat2) 
        {
            distanza = root
            .GetProperty("routes")[0]
            .GetProperty("summary")
            .GetProperty("distance")
            .GetDouble();

            durata = root
                .GetProperty("routes")[0]
                .GetProperty("summary")
                .GetProperty("duration")
                .GetDouble();
        }

        return new CalcoloRoute
        {
            DistanzaKm = distanza / 1000.0,
            DurataMinuti = durata / 60
        };
    }
}