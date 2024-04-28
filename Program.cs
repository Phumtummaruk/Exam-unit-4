using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        string yrApiUrl = "https://api.met.no/weatherapi/locationforecast/2.0/compact?lat=59.93&lon=10.73";

        string yrWeatherData = await FetchWeatherData(yrApiUrl);

        if (!string.IsNullOrEmpty(yrWeatherData))
        {
            // user input
            Console.WriteLine("Enter user measurements (temperature in Celsius):");
            double userTemperature = Convert.ToDouble(Console.ReadLine());

            // Calculate
            double yrTemperature = ParseYRTemperature(yrWeatherData);
            double difference = CalculateDifference(userTemperature, yrTemperature);
            Console.WriteLine("Difference between user temperature and YR data: " + difference + " Celsius");

            // Save result
            SaveDataAsJson("user_measurement.json", userTemperature.ToString());
            SaveDataAsJson("yr_weather_day.json", yrTemperature.ToString());
            UpdateWeatherData("weather_data_daily.json", yrTemperature.ToString());

            // Calculate weekly and monthly averages
            List<double> dailyTemperatures = ReadWeatherData("weather_data_daily.json");
            double weeklyAverage = dailyTemperatures.Average();
            double monthlyAverage = dailyTemperatures.Count > 7 ? dailyTemperatures.TakeLast(7).Average() : 0;

            UpdateWeatherData("weather_data_weekly.json", weeklyAverage.ToString());
            UpdateWeatherData("weather_data_monthly.json", monthlyAverage.ToString());
        }
        else
        {
            Console.WriteLine("Failed to fetch YR weather data. Please check the API URL.");
        }
    }

    static async Task<string> FetchWeatherData(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
    }

    static double ParseYRTemperature(string yrWeatherData)
    {
        JsonDocument doc = JsonDocument.Parse(yrWeatherData);
        JsonElement root = doc.RootElement;
        JsonElement properties = root.GetProperty("properties");
        JsonElement timeseries = properties.GetProperty("timeseries");
        JsonElement firstTimeseries = timeseries.EnumerateArray().First();
        JsonElement data = firstTimeseries.GetProperty("data");
        JsonElement instant = data.GetProperty("instant");
        JsonElement details = instant.GetProperty("details");
        double yrTemperature = details.GetProperty("air_temperature").GetDouble();

        return yrTemperature;
    }

    static double CalculateDifference(double userTemperature, double yrTemperature)
    {
        return Math.Abs(userTemperature - yrTemperature);
    }

    static void SaveDataAsJson(string fileName, string data)
    {
        File.WriteAllText(fileName, data);
        Console.WriteLine("Saved data as JSON in file: " + fileName);
    }

    static void UpdateWeatherData(string fileName, string newUserData)
    {
        if (File.Exists(fileName))
        {
            string existingData = File.ReadAllText(fileName);
            string updatedData = existingData + "," + newUserData;
            File.WriteAllText(fileName, updatedData);
            Console.WriteLine("Updated data for " + fileName);
        }
        else
        {
            File.WriteAllText(fileName, newUserData);
            Console.WriteLine("Saved data as JSON in file: " + fileName);
        }
    }

    static List<double> ReadWeatherData(string fileName)
    {
        if (File.Exists(fileName))
        {
            string existingData = File.ReadAllText(fileName);
            string[] temperatures = existingData.Split(',');
            return temperatures.Select(t => Convert.ToDouble(t)).ToList();
        }
        return new List<double>();
    }
}