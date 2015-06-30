using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Utilities
{
    class Logger
    {
        private Dictionary<string, Action> ColorCommands = new Dictionary<string, Action>
        {
            { "$RED$", ()=> { Console.ForegroundColor = ConsoleColor.Red; } },
            { "$YELLOW$", ()=> { Console.ForegroundColor = ConsoleColor.Yellow; } },
            { "$GREEN$", ()=> { Console.ForegroundColor = ConsoleColor.Green; } },
            { "$CYAN$", ()=> { Console.ForegroundColor = ConsoleColor.Cyan; } },
            { "$RESET$", ()=> { Console.ResetColor(); } }
        };
        
        // Would be really nice to just pass the class type itself to the constructor. I'll figure it out later.
        private string className = "";

        public Logger(string className)
        {
            this.className = className;
        }
        
        public void info(string message, params object[] args)
        {
            Console.Out.Write("{0,-15} ", className);
            string formatted = string.Format(message, args);
            printWithColor(formatted, Console.Out);
            //Console.Out.WriteLine(formatted);
        }

        public void error(string message, params object[] args)
        {
            Console.Out.Write("{0,-15} ", className);
            string formatted = string.Format(message, args);
            printWithColor(formatted, Console.Error);
            //Console.Error.WriteLine();
        }

        private void printWithColor(string message, TextWriter writer)
        {
            while (true)
            {
                int earliest = -1;
                string chosenKey = null;
                Action chosenAction = null;
                foreach(KeyValuePair<string, Action> entry in ColorCommands)
                {
                    int index = message.IndexOf(entry.Key);
                    
                    if(index >= 0 && (earliest == -1 || index < earliest))
                    {
                        chosenKey = entry.Key;
                        chosenAction = entry.Value;
                        earliest = index;
                    }
                }

                // If we didn't find any hits, just finish printing the rest of the message and return
                if (earliest == -1)
                {
                    writer.Write(message);
                    break;
                }
                else
                {
                    // We found some action...

                    // 1. Write all the remaining stuff up until the command we found.
                    writer.Write(message.Substring(0, earliest));

                    // 2. Do the command
                    chosenAction.Invoke();

                    // 3. Set us up 
                    message = message.Substring(earliest + chosenKey.Length);
                }

            }

            writer.WriteLine();
        }

    }
}
