using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;


namespace SpellingCorrectorWebService
{
    public class SpellingCorrectorFunction
    {
        private static int MaxEditDistance = 1;
        private static int VerBose = 0;

        public class DictionaryItem
        {
            public List<Int32> suggestions = new List<Int32>();
            public Int32 count = 0;
        }

        public class SuggestItem
        {
            public string term = string.Empty;
            public int distance = 0;
            public Int32 count = 0;

            public override bool Equals(object obj)
            {
                return Equals(term, ((SuggestItem)obj).term);
            }

            public override int GetHashCode()
            {
                return term.GetHashCode();
            }
        }

        private static Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public static List<string> wordList = new List<string>();

        // Using regular expression for parsing a line from the corpus file.
        private static IEnumerable<string> ParseWords(string text)
        {
            return Regex.Matches(text.ToLower(), @"[\w-[\d_]]+").Cast<Match>().Select(m => m.Value);
        }

        public static int MaxLength = 0;

        // Everytime there is a delete operation, the list will have a list of suggestions for the next step of edit distance.
        // Creating words list in terms of the corpus text file and the language provided.
        private static bool CreateDictionaryEntry(string key, string language)
        {
            bool result = false;
            DictionaryItem value = null;
            object valueO;

            if (dictionary.TryGetValue(language + key, out valueO))
            {
                if (valueO is Int32)
                {
                    Int32 tmp = (Int32)valueO;
                    value = new DictionaryItem();
                    value.suggestions.Add(tmp);
                    dictionary[language + key] = value;
                }
                else
                {
                    value = (valueO as DictionaryItem);
                }

                // Prevent oversize.
                if (value.count < Int32.MaxValue)
                    value.count++;
            }
            else if (wordList.Count < Int32.MaxValue)
            {
                value = new DictionaryItem();
                (value as DictionaryItem).count++;
                dictionary.Add(language + key, value as DictionaryItem);

                if (key.Length > MaxLength)
                    MaxLength = key.Length;
            }

            // Whenever read a word from the corpus, system will create edits and suggestions.
            if ((value as DictionaryItem).count == 1)
            {
                wordList.Add(key);
                Int32 keyInt = (Int32)(wordList.Count - 1);

                result = true;

                foreach (string delete in Edits(key, 0, new HashSet<string>()))
                {
                    object value2;
                    if (dictionary.TryGetValue(language + key, out value2))
                    {
                        if (value2 is Int32)
                        {
                            Int32 tmp = (Int32)value2;
                            DictionaryItem item = new DictionaryItem();
                            item.suggestions.Add(tmp);
                            dictionary[language + delete] = item;

                            if (!item.suggestions.Contains(keyInt))
                                AddLowestDistance(item, key, keyInt, delete);
                        }
                        else if (!(value2 as DictionaryItem).suggestions.Contains(keyInt))
                            AddLowestDistance(value2 as DictionaryItem, key, keyInt, delete);
                    }
                    else
                    {
                        dictionary.Add(language + delete, keyInt);
                    }
                }
            }

            return result;
        }

        // Creating words list in terms of the corpus text file provided and what kind of language.
        public static void CreateDictionary(string corpus, string language)
        {
            if (!File.Exists(corpus))
            {
                // No corpus file is availabel.
                return;
            }

            long wordCount = 0;

            using (StreamReader sr = new StreamReader(corpus))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    foreach (string key in ParseWords(line))
                    {
                        if (CreateDictionaryEntry(key, language))
                            wordCount++;
                    }
                }
            }

            wordList.TrimExcess();

            if (!DbOperation.CheckDatabaseExists("WORDS"))
            {
                DbOperation.CreateDatabase("WORDS");
                DbOperation.CreateTable();
                foreach (var item in wordList)
                {
                    DbOperation.InsertWord(item);
                }
            }
        }

        public static void GetWordList()
        {
            if (!DbOperation.CheckDatabaseExists("Words"))
            {
                DbOperation.CreateDatabase("Words");
                DbOperation.CreateTable();
            }
            else
            {
                wordList = DbOperation.GetAllWords();
            }
        }

        public static void AddLowestDistance(DictionaryItem item, string suggestion, Int32 suggestionint, string delete)
        {
            // Verbose < 2 then remove all the existed suggestions.
            if ((VerBose < 2) && (item.suggestions.Count > 0) && (wordList[item.suggestions[0]].Length - delete.Length > suggestion.Length - delete.Length))
                item.suggestions.Clear();
            // Exceeding verbose, does not suggestion the edit distance longer than request.
            if ((VerBose == 2) || (item.suggestions.Count == 0) || (wordList[item.suggestions[0]].Length - delete.Length >= suggestion.Length - delete.Length))
                item.suggestions.Add(suggestionint);
        }

        // In this algorithm, we support delete only as insert, replace are very expensive and depends on kind of language provided.
        public static HashSet<string> Edits(string word, int editDistance, HashSet<string> deletes)
        {
            editDistance++;
            if (word.Length > 1)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    string delete = word.Remove(i, 1);
                    if (deletes.Add(delete))
                    {
                        if (editDistance < MaxEditDistance)
                            Edits(delete, editDistance, deletes);
                    }
                }
            }
            return deletes;
        }

        // Look up the words in the list and do comparison in terms of the MaxEditDistance and return the suggestions.
        public static List<SuggestItem> Lookup(string input, string language, int MaxEditDistance)
        {
            if (input.Length - MaxEditDistance > MaxLength)
                return new List<SuggestItem>();

            List<string> candidates = new List<string>();
            HashSet<string> hashset1 = new HashSet<string>();

            List<SuggestItem> suggestions = new List<SuggestItem>();
            HashSet<string> hashset2 = new HashSet<string>();

            object valueO;

            candidates.Add(input);
            while (candidates.Count > 0)
            {
                string candidate = candidates[0];
                candidates.RemoveAt(0);

                if ((VerBose < 2) && (suggestions.Count > 0) && (input.Length - candidate.Length > suggestions[0].distance))
                    goto sort;
                if (dictionary.TryGetValue(language + candidate, out valueO))
                {
                    DictionaryItem value = new DictionaryItem();
                    if (valueO is Int32)
                        value.suggestions.Add((Int32)valueO);
                    else
                        value = (DictionaryItem)valueO;

                    if ((value.count > 0) && hashset2.Add(candidate))
                    {
                        SuggestItem si = new SuggestItem();
                        si.term = candidate;
                        si.count = value.count;
                        si.distance = input.Length - candidate.Length;
                        suggestions.Add(si);

                        if ((VerBose < 2) && (input.Length - candidate.Length == 0))
                            goto sort;
                    }

                    object value2;
                    foreach (int suggestionint in value.suggestions)
                    {
                        string suggestion = wordList[suggestionint];
                        if (hashset2.Add(suggestion))
                        {
                            int distance = 0;
                            if (suggestion != input)
                            {
                                if (suggestion.Length == candidate.Length)
                                    distance = input.Length - candidate.Length;
                                else if (input.Length == candidate.Length)
                                    distance = suggestion.Length - candidate.Length;
                                else
                                {
                                    int ii = 0;
                                    int jj = 0;
                                    while ((ii < suggestion.Length) && (ii < input.Length) && (suggestion[ii] == input[ii]))
                                        ii++;
                                    while ((jj < suggestion.Length - ii) && (jj < input.Length - ii) && (suggestion[suggestion.Length - jj - 1] == input[input.Length - jj - 1]))
                                        jj++;

                                    if ((ii > 0) || (jj > 0))
                                    {
                                        distance = DamerauLevenshteinDistance(suggestion.Substring(ii, suggestion.Length - ii - jj), input.Substring(ii, input.Length - ii - jj));
                                    }
                                    else
                                        distance = DamerauLevenshteinDistance(suggestion, input);
                                }
                            }

                            if ((VerBose < 2) && (suggestions.Count > 0) && (suggestions[0].distance > distance))
                                suggestions.Clear();
                            if ((VerBose < 2) && (suggestions.Count > 0) && (distance > suggestions[0].distance))
                                continue;

                            if (distance <= MaxEditDistance)
                            {
                                if (dictionary.TryGetValue(language + suggestion, out value2))
                                {
                                    SuggestItem si = new SuggestItem();
                                    si.term = suggestion;
                                    si.count = (value2 as DictionaryItem).count;
                                    si.distance = distance;
                                    suggestions.Add(si);
                                }
                            }
                        }
                    }
                }

                if (input.Length - candidate.Length < MaxEditDistance)
                {
                    if ((VerBose < 2) && (suggestions.Count > 0) && (input.Length - candidate.Length >= suggestions[0].distance))
                        continue;
                    for (int i = 0; i < candidate.Length; i++)
                    {
                        string delete = candidate.Remove(i, 1);
                        if (hashset1.Add(delete))
                            candidates.Add(delete);
                    }
                }
            }

            sort:
            if (VerBose < 2)
                suggestions.Sort((x, y) => -x.count.CompareTo(y.count));
            else
                suggestions.Sort((x, y) => 2 * x.distance.CompareTo(y.distance) - x.count.CompareTo(y.count));
            if ((VerBose == 0) && (suggestions.Count > 1))
                return suggestions.GetRange(0, 1);
            else
                return suggestions;
        }

        public static List<SuggestItem> Correct(string input, string language)
        {
            List<SuggestItem> suggestions = null;
            suggestions = Lookup(input, language, MaxEditDistance);

            return suggestions;
        }

        public static List<SuggestItem> ReadFromStdIn(string input, string language)
        {
            return Correct(input, language);
        }

        // The algorithm is a classical one, not self created which computes the shortest edit distance.
        // Whenever we get an input word and the comparison, we build the matrix for computing the edit
        // distance.
        
    
        public static Int32 DamerauLevenshteinDistance(String source, String target)
        {
            Int32 m = source.Length;
            Int32 n = target.Length;
            Int32[,] H = new Int32[m + 2, n + 2];

            Int32 INF = m + n;
            H[0, 0] = INF;
            for (Int32 i = 0; i <= m; i++)
            {
                H[i + 1, 1] = i;
                H[i + 1, 0] = INF;
            }

            for (Int32 j = 0; j <= n; j++)
            {
                H[1, j + 1] = j;
                H[0, j + 1] = INF;
            }

            SortedDictionary<Char, Int32> sd = new SortedDictionary<Char, Int32>();

            foreach (Char Letter in (source + target))
            {
                if (!sd.ContainsKey(Letter))
                    sd.Add(Letter, 0);
            }

            for (Int32 i = 1; i <= m; i++)
            {
                Int32 DB = 0;
                for (Int32 j = 1; j <= n; j++)
                {
                    Int32 i1 = sd[target[j - 1]];
                    Int32 j1 = DB;
                    if (source[i - 1] == target[j - 1])
                    {
                        H[i + 1, j + 1] = H[i, j];
                        DB = j;
                    }
                    else
                    {
                        H[i + 1, j + 1] = Math.Min(H[i, j], Math.Min(H[i + 1, j], H[i, j + 1])) + 1;
                    }

                    H[i + 1, j + 1] = Math.Min(H[i + 1, j + 1], H[i1, j1] + (i - i1 - 1) + 1 + (j - j1 - 1));
                }
                sd[source[i - 1]] = i;
            }

            return H[m + 1, n + 1];
        }
    }
}
