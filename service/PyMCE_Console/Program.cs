using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PyMCE_Core.Device;

namespace PyMCE_Console
{
    class Program
    {
        const string PromptMessage = "[exit, learn, send]> ";

        static void Main(string[] args)
        {
            var transceiver = new Transceiver();
            transceiver.Start();

            byte[] learned = null;

            Console.Write(PromptMessage);
            string line;
            while((line = Console.ReadLine()) != "exit")
            {
                switch(line)
                {
                    case "learn":
                        transceiver.Learn(out learned);
                        break;
                    case "send":
                        if(learned != null)
                            transceiver.Transmit("Both", learned);
                        else
                            Console.WriteLine("Haven't learnt anything yet!");
                        break;
                }

                Console.Write(PromptMessage);
            }

            transceiver.Stop();
        }
    }
}
