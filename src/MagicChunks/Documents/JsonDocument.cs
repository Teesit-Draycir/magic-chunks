using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MagicChunks.Core;
using MagicChunks.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MagicChunks.Documents
{
    public class JsonDocument : IDocument
    {
        private static readonly Regex JsonObjectRegex = new Regex(@"^{.+}$$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex NodeIndexEndingRegex = new Regex(@"\[\d+\]$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex NodeValueEndingRegex = new Regex(@"\[\@.+\=.+\]$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        protected readonly JObject Document;

        public JsonDocument(string source)
        {
            try
            {
                Document = (JObject)JsonConvert.DeserializeObject(source);
            }
            catch (JsonReaderException ex)
            {
                throw new ArgumentException("Wrong document format", nameof(source), ex);
            }
        }

        public void AddElementToArray(string[] path, string value)
        {
            if ((path == null) || (path.Any() == false))
                throw new ArgumentException("Path is not speicified.", nameof(path));

            if (path.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException("There is empty items in the path.", nameof(path));

            JObject current = (JObject)Document.Root;

            if (current == null)
                throw new ArgumentException("Root element is not present.", nameof(path));

            current = FindPath(path.Take(path.Length - 1), current);

            UpdateTargetArrayElement(current, path.Last(), value);
        }

        public void ReplaceKey(string[] path, string value)
        {
            if ((path == null) || (path.Any() == false))
                throw new ArgumentException("Path is not speicified.", nameof(path));

            if (path.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException("There is empty items in the path.", nameof(path));

            JObject current = (JObject)Document.Root;

            if (current == null)
                throw new ArgumentException("Root element is not present.", nameof(path));

            current = FindPath(path.Take(path.Length - 1), current);

            UpdateTargetElement(current, path.Last(), value);
        }

        public void RemoveKey(string[] path)
        {
            if ((path == null) || (path.Any() == false))
                throw new ArgumentException("Path is not speicified.", nameof(path));

            if (path.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException("There is empty items in the path.", nameof(path));

            JObject current = (JObject)Document.Root;

            if (current == null)
                throw new ArgumentException("Root element is not present.", nameof(path));

            current = FindPath(path.Take(path.Length - 1), current);
            var pathEnding = path.Last();
            if (NodeIndexEndingRegex.IsMatch(pathEnding) || NodeValueEndingRegex.IsMatch(pathEnding))
            {
                // Remove item from array
                current.GetChildPropertyValue(pathEnding)?.Remove();
            }
            else
            {
                // Remove property
                current.Remove(pathEnding);
            }
        }

        private static JObject FindPath(IEnumerable<string> path, JObject current)
        {
            foreach (string pathElement in path)
            {
                var element = current.GetChildPropertyValue(pathElement);
                if (element is JObject)
                {
                    current = (JObject)element;
                }
                else if (element is JArray)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    current[pathElement] = new JObject();
                    current = (JObject) current[pathElement];
                }
            }
            return current;
        }

        private static void UpdateTargetArrayElement(JObject current, string targetElementName, string value)
        {
            var targetElement = current.GetChildProperty(targetElementName) as JProperty;
            if ((targetElement != null) && (targetElement is JProperty) && (targetElement.Value is JArray))
            {
                if (JsonObjectRegex.IsMatch(value.Trim()))
                {
                    ((JArray)targetElement.Value).Add(JsonConvert.DeserializeObject(value));
                }
                else
                {
                    ((JArray)targetElement.Value).Add(value);
                }
            }
            else if (targetElement != null)
                throw new FormatException("Target element is not array.");
            else
            {
                var array = new JArray();
                if (JsonObjectRegex.IsMatch(value.Trim()))
                {
                    array.Add(JsonConvert.DeserializeObject(value));
                }
                else
                {
                    array.Add(value);
                }
                current.Add(targetElementName, array);
            }
        }

        private static void UpdateTargetElement(JObject current, string targetElementName, string value)
        {
            var targetElement = current.GetChildProperty(targetElementName);
            if (targetElement is JProperty)
                ((JProperty)targetElement).Value = value;
            else if (targetElement is JObject)
            {
                var targetValue = JsonConvert.DeserializeObject(value) as JObject;
                if (targetValue != null)
                    ((JObject)targetElement).Replace(targetValue);
                else
                    throw new ArgumentException("Value is not valid JSON object.", nameof(value));
            }
            else
                current.Add(targetElementName, value);
        }

        public override string ToString()
        {
            return Document?.ToString() ?? String.Empty;
        }

        public void Dispose()
        {
        }
    }
}