using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanonRPWService.DSSDCommands
{
    public class RawDssdCommand : IDisposable
    {
        public List<string> Arguments = new List<string>();
        public string Command;
        public RawDssdCommand(string _commandMessage)
        {
            string[] list = _commandMessage.Split('\n');
            if(list.Length == 0) return;
            Command = list[0];
            /*list = list.Where(val => val != list[0]).ToArray();
            foreach (string arg in list)
            {
                Arguments.Add(arg); 
            }*/
            for(int i = 1; i < list.Length; i++)
            {
                Arguments.Add(list[i]);
            }
        }
        public override string ToString()
        {
            string res = Command;
            res += "\n";
            foreach(var arg in Arguments)
            {
                res += arg;
                res += "\n";
            }
            return res;
        }

        public void Dispose()
        {
            if(Arguments.Count > 0)
            {
                Arguments.Clear();
                Command = null;                
            }
        }
    }
}
