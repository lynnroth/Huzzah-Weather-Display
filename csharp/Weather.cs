using ForecastIOPortable;
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
		const float precipThreshhold = .75F;
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

				result.AppendLine($"HOURLY_SUMMARY={WordWrap(forecast.Hourly.Summary,15)}");

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


				bool nextPrecipFound = false;
				foreach (var minute in forecast.Minutely.Minutes)
				{
					if (minute.PrecipitationProbability >= precipThreshhold )
					{
						result.AppendLine($"NEXTPRECIP_TIME={minute.Time.LocalDateTime.ToShortTimeString()}");
						result.AppendLine($"NEXTPRECIP_TYPE={minute.PrecipitationType}");
						result.AppendLine($"NEXTPRECIP_INTENSITY={minute.PrecipitationIntensity}");
						result.AppendLine($"NEXTPRECIP_PROBABILITY={minute.PrecipitationProbability}");
						nextPrecipFound = true;
						break;
					}
				}

				if (!nextPrecipFound)
				{
					foreach (var hour in forecast.Hourly.Hours)
					{
						if (hour.PrecipitationProbability >= precipThreshhold)
						{
							result.AppendLine($"NEXTPRECIP_TIME={hour.Time.LocalDateTime}");
							result.AppendLine($"NEXTPRECIP_TYPE={hour.PrecipitationType}");
							result.AppendLine($"NEXTPRECIP_INTENSITY={hour.PrecipitationIntensity}");
							result.AppendLine($"NEXTPRECIP_PROBABILITY={hour.PrecipitationProbability}");
							nextPrecipFound = true;
							break;
						}
					}
				}

				if (!nextPrecipFound)
				{
					foreach (var day in forecast.Daily.Days)
					{
						if (day.PrecipitationProbability >= precipThreshhold)
						{
							result.AppendLine($"NEXTPRECIP_TIME={day.Time.LocalDateTime}");
							result.AppendLine($"NEXTPRECIP_TYPE={day.PrecipitationType}");
							result.AppendLine($"NEXTPRECIP_INTENSITY={day.PrecipitationIntensity}");
							result.AppendLine($"NEXTPRECIP_PROBABILITY={day.PrecipitationProbability}");
							nextPrecipFound = true;
							break;
						}
					}
				}


				return result.ToString();

			};
		}

		private static async Task GetForcast(dynamic x, double latitude, double longitude)
		{
			//if (ForecastRetrieved < DateTime.Now.AddMinutes(5))
			//{
			//	return;
			//}

			ForecastApi api = new ForecastApi(x.apikey);
			forecast = await api.GetWeatherDataAsync(latitude, longitude);
			ForecastRetrieved = DateTime.Now;
		}

		/// <summary>
		/// Word wraps the given text to fit within the specified width.
		/// </summary>
		/// <param name="text">Text to be word wrapped</param>
		/// <param name="width">Width, in characters, to which the text
		/// should be word wrapped</param>
		/// <returns>The modified text</returns>
		public static string WordWrap(string text, int width)
		{
			int pos, next;
			StringBuilder sb = new StringBuilder();

			// Lucidity check
			if (width < 1)
				return text;

			// Parse each line of text
			for (pos = 0; pos < text.Length; pos = next)
			{
				// Find end of line
				int eol = text.IndexOf('\t', pos);
				if (eol == -1)
					next = eol = text.Length;
				else
					next = eol + '\t';

				// Copy this line of text, breaking into smaller lines as needed
				if (eol > pos)
				{
					do
					{
						int len = eol - pos;
						if (len > width)
							len = BreakLine(text, pos, width);
						sb.Append(text, pos, len);
						sb.Append('\t');

						// Trim whitespace following break
						pos += len;
						while (pos < eol && Char.IsWhiteSpace(text[pos]))
							pos++;
					} while (eol > pos);
				}
				else sb.Append('\t'); // Empty line
			}
			return sb.ToString();
		}

		/// <summary>
		/// Locates position to break the given line so as to avoid
		/// breaking words.
		/// </summary>
		/// <param name="text">String that contains line of text</param>
		/// <param name="pos">Index where line of text starts</param>
		/// <param name="max">Maximum line length</param>
		/// <returns>The modified line length</returns>
		private static int BreakLine(string text, int pos, int max)
		{
			// Find last whitespace in line
			int i = max;
			while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
				i--;

			// If no whitespace found, break at maximum length
			if (i < 0)
				return max;

			// Find start of whitespace
			while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
				i--;

			// Return length of text before whitespace
			return i + 1;
		}
	}
}
