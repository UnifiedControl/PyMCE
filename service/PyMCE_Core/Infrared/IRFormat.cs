using System;

namespace PyMCE.Core.Infrared
{
    public class IRFormat
    {
        public enum ProntoFormat
        {
            LearnedModulated    = 0,        // 0000
            LearnedUnmodulated  = 256,      // 0100

            RC5                 = 20480,    // 5000
            RC5x                = 20481,    // 5001

            RC6                 = 24576,    // 6000
            RC5A                = 24577     // 6001
        }

        public string Word { get; private set; }

        public ProntoFormat Format { get; private set; }

        internal IRFormat(string word, ProntoFormat format)
        {
            Word = word;
            Format = format;
        }

        public static IRFormat FromProntoWord(string word)
        {
            return new IRFormat(word, (ProntoFormat) Convert.ToInt32(word, 16));
        }
    }
}