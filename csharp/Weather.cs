using ForecastIOPortable.Models;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace csharp
{
	

    public class WeatherModule : Nancy.NancyModule
    {
		static Forecast forecast = null;
		static DateTime? ForecastRetrieved = null;

		public WeatherModule()
		{
			Get["/weather/{apikey}/{latlon}", true] = async (x, ct) =>
			{
				var parts = ((string)x.latlon).Split(',');
				double latitude = double.Parse(parts[0]);
				double longitude = double.Parse(parts[1]);
				await GetForcast(x, latitude, longitude);

				StringBuilder result = new StringBuilder();

				result.AppendLine($"CURRENT_TEMP={Math.Round(forecast.Currently.Temperature, 1, MidpointRounding.AwayFromZero)}");
				result.AppendLine($"CURRENT_HUMIDITY={forecast.Currently.Humidity * 100}");
				result.AppendLine($"CURRENT_ICON={forecast.Currently.Icon}");
				result.AppendLine($"CURRENT_SUMMARY={forecast.Currently.Summary}");

				foreach (var minute in forecast.Minutely.Minutes)
				{
					if (minute.PrecipitationProbability >= .3)
					{
						result.AppendLine($"NEXTPRECIP_TIME={minute.Time.LocalDateTime.ToShortTimeString()}");
						break;
					}
				}

				result.AppendLine($"TODAY_MAX_TEMP={forecast.Daily.Days[0].MaxTemperature}");
				result.AppendLine($"TODAY_MAX_TEMP_TIME={forecast.Daily.Days[0].MaxTemperatureTime.LocalDateTime.ToShortTimeString()}");
				result.AppendLine($"TODAY_MIN_TEMP={forecast.Daily.Days[0].MinTemperature}");
				result.AppendLine($"TODAY_MIN_TEMP_TIME={forecast.Daily.Days[0].MinTemperatureTime.LocalDateTime.ToShortTimeString()}");
				result.AppendLine($"TODAY_ICON={forecast.Daily.Days[0].Icon}");
				result.AppendLine($"TODAY_SUMMARY={forecast.Daily.Days[0].Summary}");

				result.AppendLine($"TOMORROW_MAX_TEMP={forecast.Daily.Days[1].MaxTemperature}");
				result.AppendLine($"TOMORROW_MAX_TEMP_TIME={forecast.Daily.Days[1].MaxTemperatureTime.LocalDateTime.ToShortTimeString()}");
				result.AppendLine($"TOMORROW_MIN_TEMP={forecast.Daily.Days[1].MinTemperature}");
				result.AppendLine($"TOMORROW_MIN_TEMP_TIME={forecast.Daily.Days[1].MinTemperatureTime.LocalDateTime.ToShortTimeString()}");
				result.AppendLine($"TOMORROW_ICON={forecast.Daily.Days[1].Icon}");
				result.AppendLine($"TOMORROW_SUMMARY={forecast.Daily.Days[0].Summary}");

				string alert1Title = forecast.Alerts?[0]?.Title;
				if (alert1Title != null)
				{
					result.AppendLine($"ALERT!={alert1Title}-{forecast.Alerts[0].Description}");
				}
				string alert2Title = forecast.Alerts?[1]?.Title;
				if (alert2Title != null)
				{
					result.AppendLine($"ALERT!={alert2Title}-{forecast.Alerts[1].Description}");
				}

				return result.ToString();

			};
		}

		private static async Task GetForcast(dynamic x, double latitude, double longitude)
		{
			if (ForecastRetrieved < DateTime.Now.AddMinutes(5))
			{
				return;
			}

			ForecastIOPortable.ForecastApi api = new ForecastIOPortable.ForecastApi(x.apikey);
			forecast = await api.GetWeatherDataAsync(latitude, longitude);
			ForecastRetrieved = DateTime.Now;
		}
	}
}
