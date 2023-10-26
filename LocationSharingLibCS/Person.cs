using Newtonsoft.Json.Linq;

namespace LocationSharingLibCS
{
    /// <summary>
    /// A person sharing its location as coordinates.
    /// </summary>
    public class Person
    {
        internal string? Id { get; }
        internal string? PictureUrl { get; }
        internal string? FullName { get; }
        internal string? NickName { get; }
        internal string? Latitude { get; }
        internal string? Longitude { get; }
        internal DateTime? Timestamp { get; }
        internal string? Accuracy { get; }
        internal string? Address { get; }
        internal string? CountryCode { get; }
        internal bool? Charging { get; }
        internal int? BatteryLevel { get; }

        internal Person(JToken input)
        {
            JArray data = (JArray)input;
            if (IsNullOrEmpty(data)) throw new NullReferenceException();

            if (IsNullOrEmpty(data[0]))
            {
                // AuthenticatedPersonのとき

                if (data.Count < 2) throw new Exception($"{nameof(data)} is too small range.");
                JArray data1;
                data1 = (JArray)data[1];
                if (IsNullOrEmpty(data1)) throw new NullReferenceException();

                Id = null;
                PictureUrl = null;
                FullName = null;
                NickName = null;

                if (data1.Count < 8) throw new Exception($"{nameof(data)}[1] is too small range.");
                JArray data11;
                data11 = (JArray)data1[1];
                if (IsNullOrEmpty(data11)) throw new NullReferenceException();

                if (data11.Count < 3) throw new Exception($"{nameof(data)}[1][1] is too small range.");
                Latitude = (string?)data11[2];
                Longitude = (string?)data11[1];
                Timestamp = GetDatetime(long.Parse((string?)data1[2] ?? "0"));
                Accuracy = (string?)data1[3] ?? null;
                Address = (string?)data1[4] ?? null;
                CountryCode = (string?)data1[6] ?? null;

                Charging = null;
                BatteryLevel = null;
            }
            else
            {
                // SharedPersonのとき
                if (data.Count < 14) throw new Exception($"{nameof(data)} is too small range.");

                JArray data0 = (JArray)data[0];
                if (IsNullOrEmpty(data0)) throw new NullReferenceException();
                if (data0.Count < 4) throw new Exception($"{nameof(data)}[0] is too small range.");

                Id = (string?)data0[0] ?? null;
                PictureUrl = (string?)data0[1] ?? null;
                FullName = (string?)data0[3] ?? null;

                JArray data1 = (JArray)data[1];
                if (IsNullOrEmpty(data1)) throw new NullReferenceException();
                if (data1.Count < 7) throw new Exception($"{nameof(data)}[1] is too small range.");

                JArray data11 = (JArray)data1[1];
                if (IsNullOrEmpty(data11)) throw new NullReferenceException();
                if (data11.Count < 3) throw new Exception($"{nameof(data)}[1][1] is too small range.");
                Latitude = (string?)data11[2];
                Longitude = (string?)data11[1];

                Timestamp = GetDatetime(long.Parse((string?)data1[2] ?? "0"));
                Accuracy = (string?)data1[3] ?? null;
                Address = (string?)data1[4] ?? null;
                CountryCode = (string?)data1[6] ?? null;

                JArray data6 = (JArray)data[6];
                if (IsNullOrEmpty(data6)) throw new NullReferenceException();
                if (data6.Count < 4) throw new Exception($"{nameof(data)}[6] is too small range.");
                NickName = (string?)data6[3] ?? null;

                JArray data13 = (JArray)data[13];
                if (IsNullOrEmpty(data13)) throw new NullReferenceException();
                if (data13.Count < 1) throw new Exception($"{nameof(data)}[13] is too small range.");

                if (((string?)data13[0] ?? string.Empty) == "0")
                {
                    Charging = false;
                }
                else if (((string?)data13[0] ?? string.Empty) == "1")
                {
                    Charging = true;
                }
                else
                {
                    Charging = null;
                }

                if (1 < data13.Count)
                {
                    BatteryLevel = int.Parse((string?)data13[1] ?? string.Empty);
                }
            }
        }

        static private bool IsNullOrEmpty(JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == string.Empty) ||
                   (token.Type == JTokenType.Null);
        }

        static private DateTime GetDatetime(long timestamp)
        {
            if (timestamp == 0) throw new NullReferenceException();
            DateTimeOffset retOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return retOffset.LocalDateTime;
        }
    }
}
