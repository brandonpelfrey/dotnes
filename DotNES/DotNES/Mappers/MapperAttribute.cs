using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNES.Mappers
{
    [AttributeUsage(AttributeTargets.Class)]
    class MapperAttribute : Attribute
    {
        public string name { get; }
        public int number { get; }

        public MapperAttribute(string name, int number)
        {
            this.name = name;
            this.number = number;
        }
    }
}
