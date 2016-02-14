using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpellingCorrectorWebService;

namespace UnitTestSpellingCorrector
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethodCreateDictionary()
        {
            // Invalid source file for creating the wordlist
            string path = "~/source/souce.txt";
            string language = "English";

            SpellingCorrectorFunction.CreateDictionary(path, language);
            Assert.AreEqual(SpellingCorrectorFunction.wordList.Count, 0);
        }

        [TestMethod]
        public void TestMethodLookup()
        {
            // Check empty input
            string input = string.Empty;
            string language = "English";
            int maxEditDistance = 2;

            var result = SpellingCorrectorFunction.Lookup(input, language, maxEditDistance);
            Assert.AreEqual(result.Count, 0);
        }

        [TestMethod]
        public void TestMethodCorrect()
        {
            string input = "txt";
            string language = "English";

            // Valid input, invalid word, check response containers.
            var result = SpellingCorrectorFunction.Correct(input, language);
            Assert.AreEqual(result.Count, 0);
        }
    }
}
