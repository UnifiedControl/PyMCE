using PyMCE.Core.Infrared;
namespace PyMCE_Debug
{
    public class Code
    {
        public int Index { get; private set; }
        public string ProntoFormat { get; private set; }
        public string ProntoCode { get; private set; }

        public static Code FromIRCode(int index, IRCode irCode)
        {
            var prontoCode = irCode.ToProntoString();
            return new Code
                       {
                           Index = index,
                           ProntoCode = prontoCode,
                           ProntoFormat = IRFormat.FromProntoWord(prontoCode.Substring(0, 4)).Format.ToString()
                       };
        }
    }
}
