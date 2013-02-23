using System;

namespace PyMCE.Core.Infrared
{
    public class IRFormat
    {
        public string Word { get; private set; }

        public Pronto.CodeFormat Format { get; private set; }

        internal IRFormat(string word, Pronto.CodeFormat format)
        {
            Word = word;
            Format = format;
        }

        public static IRFormat FromProntoWord(string word)
        {
            return new IRFormat(word, (Pronto.CodeFormat) Convert.ToInt32(word, 16));
        }
    }
}