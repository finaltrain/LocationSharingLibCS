﻿using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocationSharingLibCS
{
    // Original Copyright 2017 Costas Tyfoxylos
    // Copyright 2023 finaltrain

    /// <summary>
    /// A library to retrieve coordinates from an google account that has been shared locations of other accounts.
    /// </summary>
    public class LocationSharing
    {
        readonly static string[] VALID_COOKIE_NAMES = new string[] { "__Secure-1PSID", "__Secure-3PSID" };
        static string Languages = "en";
        static string CountryCode = "us";

        static readonly List<GoogleCookie> cookies = new();

        static List<Person> People = new();
        static Person? AuthenticatedPerson;

        /// <summary>
        /// Constructor when using Cookie Data
        /// </summary>
        /// <param name="cookiesData">Set cookie data with StreamReader</param>
        /// <param name="language">Select language. Default is English. ex) en, ja, zh...</param>
        /// <param name="countryCode">Select Country Code. Default is USA. ex) us, ja, cn...</param>
        /// <exception cref="InvalidDataException">Cookie Data must contain cookies named "__Secure-1PSID" and "__Secure-3PSID"</exception>
        public LocationSharing(StreamReader cookiesData, string language = "en", string countryCode = "us")
        {
            Languages = language;
            CountryCode = countryCode;

            // Retrieve cookies from StreamReader  CookiesData and stick them in List<GoogleCookie> cookies
            List<string> cookieEntries = new();

            while (!cookiesData.EndOfStream)
            {
                string line = cookiesData.ReadLine() ?? string.Empty;

                if (line != string.Empty && line.IndexOf('#') != 0)
                {
                    cookieEntries.Add(line);
                }
            }

            foreach (var entry in cookieEntries)
            {
                cookies.Add(new GoogleCookie(entry));
            }

            foreach (var validName in VALID_COOKIE_NAMES)
            {
                bool isMissing = true;
                foreach (var cookie in cookies)
                {
                    if (validName == cookie.Name)
                    {
                        isMissing = false;
                        break;
                    }
                }
                if (isMissing) throw new InvalidDataException($"Missing {validName} cookies!");
            }

            try
            {
                JToken data = GetData();
            }
            catch (HttpRequestException)
            {
                // HttpRequestException is not allowed here.
                // It's not good to have connection problems at the time of initialization.
                throw;
            }
        }

        /// <summary>
        /// Constructor when using Cookie File
        /// </summary>
        /// <param name="cookiesFilePath">Set cookie file path. absolute path or relative path.</param>
        /// <param name="language">Select language. Default is English. ex) en, ja, zh...</param>
        /// <param name="countryCode">Select Country Code. Default is USA. ex) us, ja, cn...</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException">Cookie Data must contain cookies named "__Secure-1PSID" and "__Secure-3PSID"</exception>
        public LocationSharing(string cookiesFilePath, string language = "en", string countryCode = "us")
        {
            Languages = language;
            CountryCode = countryCode;

            // Retrieve cookies from StreamReader  CookiesData and stick them in List<GoogleCookie> cookies

            List<string> cookieEntries = new();

            if (!Path.IsPathRooted(cookiesFilePath))
            {
                cookiesFilePath = Path.GetFullPath(cookiesFilePath);
            }

            if (!File.Exists(cookiesFilePath)) throw new FileNotFoundException($"Path:{cookiesFilePath} could not found.");

            using (StreamReader sr = new(cookiesFilePath, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine() ?? string.Empty;

                    if (line != string.Empty && line.IndexOf('#') != 0)
                    {
                        cookieEntries.Add(line);
                    }
                }

                foreach (var entry in cookieEntries)
                {
                    cookies.Add(new GoogleCookie(entry));
                }
                foreach (var validName in VALID_COOKIE_NAMES)
                {
                    bool isMissing = true;
                    foreach (var cookie in cookies)
                    {
                        if (validName == cookie.Name)
                        {
                            isMissing = false;
                            break;
                        }
                    }
                    if (isMissing) throw new InvalidDataException($"Missing either of {validName} cookies!");
                }
            }

            try
            {
                JToken data = GetData();
            }
            catch (HttpRequestException)
            {
                // HttpRequestException is also not allowed here.
                // It is not a good idea to have connection problems at the time of initialization.
                throw;
            }
        }

        /// <summary>
        /// Get data from Google maps
        /// </summary>
        static private HttpResponseMessage GetServerResponse()
        {
            Uri baseAddress = new("https://www.google.com/maps/rpc/locationsharing/");
            CookieContainer cookieContainer = new();
            using (HttpClientHandler handler = new() { CookieContainer = cookieContainer })
            using (HttpClient client = new(handler) { BaseAddress = baseAddress })
            {
                FormUrlEncodedContent content = new(new List<KeyValuePair<string, string>>()
                        {
                            new KeyValuePair<string, string>("authuser", "2" ),
                            new KeyValuePair<string, string>("hl", Languages),
                            new KeyValuePair<string, string>("gl", CountryCode),
                                // pb holds the information about the rendering of the map and
                                // it is irrelevant with the location sharing capabilities.
                                // the below info points to google's headquarters.
                            new KeyValuePair<string, string>("pb", "!1m7!8m6!1m3!1i14!2i8413!3i5385!2i6!3x4095" +
                                      "!2m3!1e0!2sm!3i407105169!3m7!2sen!5e1105!12m4" +
                                      "!1e68!2m2!1sset!2sRoadmap!4e1!5m4!1e4!8m2!1e0!" +
                                      "1e1!6m9!1e12!2i2!26m1!4b1!30m1!" +
                                      "1f1.3953487873077393!39b1!44e1!50e0!23i4111425")

                        });
                foreach (var cookie in cookies)
                {
                    cookieContainer.Add(baseAddress, new Cookie(cookie.Name, cookie.Value));
                }
                return client.GetAsync("read?" + content.ReadAsStringAsync().Result).Result;
            }
        }

        /// <summary>
        /// parse json
        /// </summary>
        /// <exception cref="Exception">parse error</exception>
        static private JToken ParseLocationData(string data)
        {
            JToken ret;
            try
            {
                ret = JToken.Parse(data);
            }
            catch (Exception)
            {
                throw new Newtonsoft.Json.JsonException($"Received invalid data: {nameof(data)}, cannot parse properly.");
            }
            return ret;
        }

        /// <summary>
        /// Get data from Google maps and parse json
        /// </summary>
        /// <exception cref="HttpRequestException">parse error</exception>
        static private JToken GetData()
        {
            HttpResponseMessage response;
            try
            {
                response = GetServerResponse();
            }
            catch (HttpRequestException)
            {
                throw;
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(response.ReasonPhrase);
            }

            string json = response.Content.ReadAsStringAsync().Result;
            // XXX : Countermeasure against Google returning strange Json
            string matchedJson = Regex.Match(json, @"(?=\[).*(?<=\])").Value;

            JToken data = ParseLocationData(matchedJson);

            string? authField;
            try
            {
                authField = (string?)data[6];
            }
            catch (Exception)
            {
                throw new InvalidDataException("Could not read 7th field of data, it seems invalid data");
            }

            if (authField is null || authField == "GgA=")
            {
                throw new InvalidDataException("Does not seem we have a valid session.");
            }

            return data;
        }

        /// <summary>
        /// Update all people that share their location with this account.
        /// </summary>
        /// <returns>if success, return true</returns>
        static public bool UpdateSharedPeople()
        {
            List<Person> temp = new();
            try
            {
                JToken data = GetData();

                JToken? sharedPeopleData = data[0];

                if (sharedPeopleData is null) return false;

                foreach (JToken sharedPersonData in sharedPeopleData)
                {
                    temp.Add(new Person(sharedPersonData));
                }

            }
            catch (Exception)
            {
                return false;
            }
            People = temp;
            return true;
        }

        /// <summary>
        /// Update all people that share their location with this account.
        /// </summary>
        /// <returns>if success, return true</returns>
        static public bool UpdateSharedPeople(JToken arg)
        {
            List<Person> temp = new();
            try
            {
                JToken data = arg[0] ?? string.Empty;

                JToken? sharedPeopleData = data[0];
                if (sharedPeopleData is null) return false;

                foreach (JToken sharedPersonData in sharedPeopleData)
                {
                    temp.Add(new Person(sharedPersonData));
                }
            }
            catch (Exception)
            {
                return false;
            }
            People = temp;
            return true;
        }

        /// <summary>
        /// Return all people that share their location with this account.
        /// </summary>
        static public List<Person> GetSharedPeople()
        {
            return People;
        }

        /// <summary>
        /// Update the person associated with this account.
        /// </summary>
        /// <returns>if success, return true</returns>
        static public bool UpdateAuthenticatedPerson()
        {
            try
            {
                JToken data = GetData();

                // NOTE : I don't know which is faster, casting to JArray and using Count or using LINQ's Count.
                if (((JArray)data).Count < 10) return false;
                JToken authenticatedPersonData = data[9] ?? string.Empty;
                AuthenticatedPerson = new Person(authenticatedPersonData);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update the person associated with this account.
        /// </summary>
        /// <returns>if success, return true</returns>
        static public bool UpdateAuthenticatedPerson(JToken arg)
        {
            try
            {
                JToken data = arg[0] ?? string.Empty;

                // NOTE : I don't know which is faster, casting to JArray and using Count or using LINQ's Count.
                if (((JArray)data).Count < 10) return false;
                JToken authenticatedPersonData = data[9] ?? string.Empty;
                AuthenticatedPerson = new Person(authenticatedPersonData);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Return the person associated with this account.
        /// </summary>
        /// <return>Returns null if the value has never been retrieved</return>
        static public Person? GetAuthenticatedPerson()
        {
            return AuthenticatedPerson;
        }

        /// <summary>
        /// Update all people sharing their location.
        /// </summary>
        /// <returns>if success, return true</returns>
        static public bool UpdateAllPeople()
        {
            try
            {
                JToken data = GetData();
                UpdateAuthenticatedPerson(data);
                UpdateSharedPeople(data);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Return all people sharing their location.
        /// </summary>
        static public List<Person> GetAllPeople()
        {
            List<Person> ret = new();
            if (AuthenticatedPerson is not null) ret.Add(AuthenticatedPerson);
            ret.AddRange(People);
            return ret;
        }

        /// <summary>
        /// Retrieves a person by nickname.
        /// </summary>
        /// <exception cref="ArgumentException">Nickname not found</exception>
        static public Person GetPersonByNickName(string nickname)
        {
            foreach (var person in People)
            {
                if (person.NickName == nickname) return person;
            }
            throw new ArgumentException($"Nickname : {nickname} is not found.");
        }

        /// <summary>
        /// Retrieves a person by full name.
        /// </summary>
        /// <exception cref="ArgumentException">Fullname not found</exception>
        static public Person GetPersonByFullName(string fullname)
        {
            foreach (var person in People)
            {
                if (person.FullName == fullname) return person;
            }
            throw new ArgumentException($"Fullname : {fullname} is not found.");
        }

        /// <summary>
        /// Retrieves a person's coordinates by nickname.
        /// </summary>
        static public (string?, string?) GetCoordinatesByNickName(string nickname)
        {
            Person person = GetPersonByNickName(nickname);
            return (person.Latitude, person.Longitude);
        }

        /// <summary>
        /// Retrieves a person's coordinates by full name.
        /// </summary>
        static public (string?, string?) GetCoordinatesByFullName(string fullname)
        {
            Person person = GetPersonByFullName(fullname);
            return (person.Latitude, person.Longitude);
        }

        /// <summary>
        /// Retrieves the authenticated person's coordinates.
        /// </summary>
        static public (string?, string?) GetCoordinatesOfAuthenticatedPerson()
        {
            if (AuthenticatedPerson is null) return (null, null);
            return (AuthenticatedPerson.Latitude, AuthenticatedPerson.Longitude);
        }

        /// <summary>
        /// Retrieves a person's latitude by nickname.
        /// </summary>
        static public string? GetLatitudeByNickName(string nickname)
        {
            Person person = GetPersonByNickName(nickname);
            return person.Latitude;
        }

        /// <summary>
        /// Retrieves a person's latitude by full name.
        /// </summary>
        static public string? GetLatitudeByFullName(string fullname)
        {
            Person person = GetPersonByFullName(fullname);
            return person.Latitude;
        }

        /// <summary>
        /// Retrieves the authenticated person's latitude.
        /// </summary>
        static public string? GetLatitudeOfAuthenticatedPerson()
        {
            if (AuthenticatedPerson is null) return null;
            return AuthenticatedPerson.Latitude;
        }

        /// <summary>
        /// Retrieves a person's longitude by nickname.
        /// </summary>
        static public string? GetLongitudeByNickName(string nickname)
        {
            Person person = GetPersonByNickName(nickname);
            return person.Longitude;
        }

        /// <summary>
        /// Retrieves a person's longitude by full name.
        /// </summary>
        static public string? GetLongitudeByFullName(string fullname)
        {
            Person person = GetPersonByFullName(fullname);
            return person.Longitude;
        }

        /// <summary>
        /// Retrieves the authenticated person's latitude by name.
        /// </summary>
        static public string? GetLongitudeOfAuthenticatedPerson()
        {
            if (AuthenticatedPerson is null) return null;
            return AuthenticatedPerson.Longitude;
        }

        /// <summary>
        /// Retrieves a person's time in unix format by nickname.
        /// </summary>
        static public DateTime? GetTimeStampByNickName(string nickname)
        {
            Person person = GetPersonByNickName(nickname);
            return person.Timestamp;
        }

        /// <summary>
        /// Retrieves a person's time in unix format by full name.
        /// </summary>
        static public DateTime? GetTimestampByFullName(string fullname)
        {
            Person person = GetPersonByFullName(fullname);
            return person.Timestamp;
        }

        /// <summary>
        /// Retrieves the authenticated person's time in unix format by name.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        static public DateTime? GetTimestampOfAuthenticatedPerson()
        {
            if (AuthenticatedPerson is null) return null;
            return AuthenticatedPerson.Timestamp;
        }
    }
}