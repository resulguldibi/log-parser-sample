using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace log_parser_sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://gist.githubusercontent.com/bss/6dbc7d4d6d2860c7ecded3d21098076a/raw/244045d24337e342e35b85ec1924bca8425fce2e/sample.small.log")
            });

            response.EnsureSuccessStatusCode();

            var rawLogContent = await response.Content.ReadAsStringAsync();

            string[] lines = rawLogContent.Split("\n").Where(e => e != null && e != string.Empty).ToArray();

            IDictionary<string, long> segmentCounters = new Dictionary<string, long>();

            if (lines != null && lines.Length > 0)
            {
                string key = "path";
                string pattern = $"{key}=(\\S*)(\\s*)";
                Regex regex = new Regex(pattern);
                bool isExist;
                string value = string.Empty;
                List<string> segmentsWithoutDynamicValues;

                foreach (var line in lines)
                {
                    (isExist, value) = getMatch(line, regex, 1);

                    if (isExist)
                    {
                        if (value.Contains("/"))
                        {
                            string[] segments = value.Split("/").Where(e => e != null && e != string.Empty).ToArray();

                            if (segments != null && segments.Length > 0)
                            {
                                segmentsWithoutDynamicValues = new List<string>();

                                foreach (var segment in segments)
                                {
                                    Regex dynamicSegmentRegex = new Regex("(\\D)");
                                    if (dynamicSegmentRegex.IsMatch(segment))
                                    {
                                        segmentsWithoutDynamicValues.Add(segment);
                                    }
                                    else
                                    {
                                        segmentsWithoutDynamicValues.Add("*");
                                    }
                                }

                                string actualSegment = string.Join("/", segmentsWithoutDynamicValues);

                                if (!segmentCounters.ContainsKey(actualSegment))
                                {
                                    segmentCounters.Add(actualSegment, 1);
                                }
                                else
                                {
                                    segmentCounters[actualSegment] = segmentCounters[actualSegment] + 1;
                                }
                            }
                        }
                    }
                }
            }

            if (segmentCounters != null && segmentCounters.Keys.Count > 0)
            {
                foreach (KeyValuePair<string, long> segmentCounter in segmentCounters)
                {
                    Console.WriteLine($"segment : {segmentCounter.Key}, count : {segmentCounter.Value}");
                }

                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine($"log line count : {lines.Length}, segment total hit count : {segmentCounters.Values.Sum()}");
            }

            Console.ReadLine();
        }

        private static (bool, string) getMatch(string line, Regex regex, int matchIndex)
        {
            Match m = regex.Match(line);

            string value = string.Empty;
            bool isExist = false;

            if (m.Groups != null && m.Groups.Count > matchIndex)
            {
                isExist = true;
                value = m.Groups[matchIndex].Value;
            }

            return (isExist, value);
        }
    }
}
