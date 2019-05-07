using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;

namespace UberIndexUtilities
{

    public static class UberUtilities
    {
        public static long ExtractId(byte [] json)
        {
            const string id = "\"Id\":";

            var data = System.Text.Encoding.Default.GetString(json);

            var ind = data.IndexOf(id);
            if(ind > -1)
            {
                var ind2 = data.IndexOf(',', ind);

                if (ind2 == -1)
                {
                    ind2 = data.IndexOf('}', ind);
                }

                var idString = data.Substring(ind + id.Length, ind2 - id.Length - ind);

                if(Int64.TryParse(idString, out long result))
                {
                    return result;
                }
            }

            return -1;
        }

        public static byte[] MergeData(byte[] attribute, byte[] evaluate)
        {
            var attributeObject = JObject.Parse(System.Text.Encoding.Default.GetString(attribute));
            var evaluateObject = JObject.Parse(System.Text.Encoding.Default.GetString(evaluate));

            attributeObject.Merge(evaluateObject, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

            var result = new JObject();

            foreach(var j in attributeObject)
            {
                switch(j.Key)
                {
                    case "F":
                    case "J":
                    case "C":
                        if(attributeObject["Ty"].ToString() != "0")
                        {
                            result.Add("R" + j.Key, new JValue("@*%!" + JsonConvert.SerializeObject(j.Value, Formatting.None) + "@*%!"));
                        }
                        else
                        {
                            result.Add(j.Key, j.Value);
                        }
                        break;

                    case "Id":
                    case "Ty":
                    case "Pt":
                    case "L":
                    case "Y":
                    case "D":
                    case "DC":
                    case "CC":
                    case "ECC":
                    case "AA":
                    case "CI":
                    case "RId":
                    case "DAuN":
                    case "AuN":
                    case "LKA":
                    case "DAfN":
                    case "AfN":
                    case "DFN":
                    case "FN":
                    case "DJN":
                    case "JN":
                    case "DCN":
                    case "CN":
                    case "DCIN":
                    case "CIN":
                    case "CIL":
                    case "CISD":
                    case "CIED":
                    case "CIARD":
                    case "CISDD":
                    case "CINDD":
                    case "CIFVD":
                    case "PCS":
                    case "CD":
                    case "FL":
                    case "FC":
                    case "FP":
                        result.Add(j.Key, j.Value);
                        break;

                    case "W":
                        break;

                    case "Ti":
                        result.Add(j.Key, j.Value);
                        var s = j.Value.ToString();
                        var tin1 = CreateAllStringNgrams(s, 1, 1, false);
                        var tin2 = CreateAllStringNgrams(s, 2, 2, false);

                        if (tin1 != null)
                        {
                            result.Add("TiN1", new JArray(tin1));
                        }

                        if (tin2 != null)
                        {
                            result.Add("TiN2", new JArray(tin2));
                        }

                        break;

                    default:
                        if(j.Key == "ANF")
                        {
                            foreach(var anf in (JArray)j.Value)
                            {
                                foreach(var matching in attributeObject["AA"].Where(a => a["S"].ToString() == anf["S"].ToString()))
                                {
                                    var jo = (JObject)matching;

                                    if (anf["FN"] != null)
                                    {
                                        jo.Add("AFN", new JValue(NormalizeString(anf["FN"].ToString())));
                                    }
                                    if (anf["LN"] != null)
                                    {
                                        jo.Add("ALN", new JValue(NormalizeString(anf["LN"].ToString())));
                                    }
                                }
                            }
                        }

                        switch(j.Value.Type)
                        {
                            case JTokenType.String:
                            case JTokenType.Integer:
                            case JTokenType.Float:
                            case JTokenType.Boolean:
                                result.Add(j.Key, j.Value);
                                break;

                            default:
                                result.Add(j.Key, new JValue("@*%!" + JsonConvert.SerializeObject(j.Value, Formatting.None) + "@*%!"));
                                break;
                        }
                        break;
                }
            }

            return System.Text.Encoding.UTF8.GetBytes(result.ToString(Newtonsoft.Json.Formatting.None));
        }

        public static List<string> CreateAllStringNgrams(string input, int ngramMinSize = 1, int ngramMaxSize = -1, bool removeFirst = false)
        {
            HashSet<string> stopWords = new HashSet<string>(new string[] { "and", "for", "are", "from", "have", "results", "based", "between", "can", "has", "analysis", "been", "not", "method", "also", "new", "its", "all", "but", "during", "after", "into", "other", "our", "non", "present", "most", "only", "however", "associated", "compared", "des", "related", "proposed", "about", "each", "obtained", "increased", "had", "among", "due", "how", "out", "les", "los", "abstract", "del", "many", "der", "including", "could", "report", "cases", "possible", "further", "given", "result", "las", "being", "like", "any", "made", "because", "discussed", "known", "recent", "findings", "reported", "considered", "described", "although", "available", "particular", "provides", "improved", "here", "need", "improve", "analyzed", "either", "produced", "demonstrated", "evaluated", "provided", "did", "does", "required", "before", "along", "presents", "having", "much", "near", "demonstrate", "iii", "often", "making", "the", "that", "with", "this", "were", "was", "which", "study", "using", "these", "their", "used", "than", "use", "such", "when", "well", "some", "through", "there", "under", "they", "within", "will", "while", "those", "various", "where", "then", "very", "who", "und", "should", "thus", "suggest", "them", "therefore", "since", "une", "what", "whether", "una", "von", "would", "of", "in", "a", "to", "is", "on", "by", "as", "de", "an", "at", "be", "we", "or", "s", "it", "la", "e", "en", "i", "no", "et", "el", "do", "up", "se", "un", "ii" });

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var words = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var allNgrams = new List<string>();

            for (int ngramPosition = 0, ngramSize = ngramMinSize; ngramSize <= (ngramMaxSize == -1 ? words.Length : ngramMaxSize); ngramPosition++)
            {
                if (ngramPosition + ngramSize > words.Length)
                {
                    ngramSize++;
                    ngramPosition = -1;
                }
                else
                {
                    var ngram = string.Join(" ", words.Skip(ngramPosition).Take(ngramSize));

                    if (ngram.Length > 2 && !stopWords.Contains(ngram))
                    {
                        allNgrams.Add(ngram);
                    }
                }
            }

            if (allNgrams.Count < 2)
            {
                // If there is only a single ngram it means it should match the input, so return null
                return null;
            }

            if (removeFirst)
            {
                // This case is used when the field we're creating ngrams for is also fully represented w/prefix completions
                // In that case we want to ensure there is not index overlap between the first ngram and prefix completions w/the field
                allNgrams = allNgrams.Where(a => a != allNgrams.First()).ToList();
            }

            return allNgrams.Distinct().ToList();
        }

        /// <summary>
        /// Implements the text normalization used by all academic entity "normalized" fields
        /// </summary>
        /// <param name="textToNormalize">Text to be normalized</param>
        /// <param name="charactersToBeRetained">Regular expression of characters to be maintained, e.g. "[^\w\s]"</param>
        /// <returns>Normalized text string</returns>
        public static string NormalizeString(string textToNormalize, string charactersToBeRetained = @"[^\w\s]")
        {
            if (string.IsNullOrEmpty(textToNormalize))
                return textToNormalize;

            textToNormalize = WebUtility.HtmlDecode(textToNormalize);
            textToNormalize = Regex.Replace(textToNormalize, charactersToBeRetained, " ");  // retain all custom characters

            StringBuilder sb = new StringBuilder(textToNormalize.Length);
            bool stringModified = false;
            bool isLastCharWhiteSpace = true;
            foreach (char c in textToNormalize)
            {
                if (euroEnglishCharMapping.ContainsKey(c))
                {
                    // replace special characters with english characters
                    var newStr = euroEnglishCharMapping[c];
                    if (newStr.Length > 0)
                    {
                        sb.Append(newStr);
                        isLastCharWhiteSpace = false;
                    }
                    stringModified = true;
                }
                else if (romanEnglishCharMapping.ContainsKey(c))
                {
                    // replace Roman characters with english characters
                    sb.Append(romanEnglishCharMapping[c]);
                    isLastCharWhiteSpace = false;
                    stringModified = true;
                }
                else if (char.IsWhiteSpace(c) || char.IsControl(c) || char.IsSurrogate(c))
                {
                    // replace multiple consecutive spaces with single space
                    if (isLastCharWhiteSpace)
                    {
                        stringModified = true;
                    }
                    else
                    {
                        sb.Append(' ');
                        stringModified = stringModified || (c != ' ');
                        isLastCharWhiteSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    isLastCharWhiteSpace = false;
                }
            }
            textToNormalize = (stringModified ? sb.ToString() : textToNormalize);

            return textToNormalize.Trim().ToLower();    // return the trimed lowercase string
        }

        #region European to English character mapping
        public static Dictionary<char, string>
            euroEnglishCharMapping = new Dictionary<char, string>
                                        {
                                            { '¡', "i" },
                                            { '¿', "?" },
                                            { 'Ä', "A" },
                                            { 'Å', "A" },
                                            { 'ä', "a" },
                                            { 'ª', "a" },
                                            { 'À', "A" },
                                            { 'Á', "A" },
                                            { 'Ã', "A" },
                                            { 'à', "a" },
                                            { 'á', "a" },
                                            { 'ã', "a" },
                                            { 'å', "a" },
                                            { 'Æ', "AE" },
                                            { 'æ', "ae" },
                                            { 'Ç', "C" },
                                            { 'Č', "C" },
                                            { 'Ć', "C" },
                                            { 'ç', "c" },
                                            { 'č', "c" },
                                            { 'ć', "c" },
                                            { 'È', "E" },
                                            { 'É', "E" },
                                            { 'Ê', "E" },
                                            { 'Ë', "E" },
                                            { 'Ε', "E" },
                                            { 'è', "e" },
                                            { 'é', "e" },
                                            { 'ê', "e" },
                                            { 'ë', "e" },
                                            { 'ę', "e" },
                                            { 'ε', "e" },
                                            { 'ğ', "g" },
                                            { 'Ì', "I" },
                                            { 'Í', "I" },
                                            { 'Î', "I" },
                                            { 'Ï', "I" },
                                            { 'İ', "I" },
                                            { 'ì', "i" },
                                            { 'í', "i" },
                                            { 'î', "i" },
                                            { 'ï', "i" },
                                            { 'ı', "i" },
                                            { 'ℓ', "l" },
                                            { 'ł', "l" },
                                            { 'Ñ', "N" },
                                            { 'ń', "n" },
                                            { 'ñ', "n" },
                                            { 'ô', "o" },
                                            { 'º', "o" },
                                            { 'Ò', "O" },
                                            { 'Ó', "O" },
                                            { 'Ô', "O" },
                                            { 'Õ', "O" },
                                            { 'Ö', "O" },
                                            { 'Ø', "O" },
                                            { 'ò', "o" },
                                            { 'ó', "o" },
                                            { 'õ', "o" },
                                            { 'ö', "o" },
                                            { 'ø', "o" },
                                            { 'Š', "S" },
                                            { 'ş', "s" },
                                            { 'š', "s" },
                                            { 'ß', "s" },
                                            { 'Û', "U" },
                                            { 'Ù', "U" },
                                            { 'Ú', "U" },
                                            { 'Ü', "U" },
                                            { 'ù', "u" },
                                            { 'ú', "u" },
                                            { 'û', "u" },
                                            { 'ü', "u" },
                                            { 'ÿ', "y" },
                                            { 'ż', "z" },
                                            { '\x30C', "" },
                                            { '\x301', "" }
                                        };
        #endregion

        #region Romain characters to English character mapping
        public static Dictionary<char, string>
            romanEnglishCharMapping = new Dictionary<char, string>
                                        {
                                            { 'Ⅰ', "I" },
                                            { 'Ⅱ', "II" },
                                            { 'Ⅲ', "III" },
                                            { 'Ⅳ', "IV" },
                                            { 'Ⅴ', "V" },
                                            { 'Ⅵ', "VI" },
                                            { 'Ⅶ', "VII" },
                                            { 'Ⅷ', "VIII" },
                                            { 'Ⅸ', "IX" },
                                            { 'Ⅹ', "X" },
                                            { 'Ⅺ', "XI" },
                                            { 'Ⅻ', "XII" }
                                        };
        #endregion
    }
}
