using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreferenceSystem.Generators
{
    public class IntArrayGenerator
    {
        public delegate string IntToStringConversion(string prefKey, int value);

        protected List<int> Values { get; private set; }
        protected List<string> StringRepresentation { get; private set; }

        public IntArrayGenerator()
        {
            Values = new List<int>();
            StringRepresentation = new List<string>();
        }

        public void Clear()
        {
            Values.Clear();
            StringRepresentation.Clear();
        }

        public void Add(int value, string representation = null)
        {
            Values.Add(value);
            StringRepresentation.Add(representation == null ?
                value.ToString() : representation);
        }

        public void AddRange(
            int minValueInclusive,
            int maxValueInclusive,
            int stepSize,
            string prefKey,
            IntToStringConversion intToString)
        {
            for (int i = minValueInclusive; i <= maxValueInclusive; i += stepSize)
            {
                string str = intToString(prefKey, i);
                Add(i, str);
            }
        }

        public List<int> GetAsList()
        {
            return Values;
        }

        public int[] GetArray()
        {
            return Values.ToArray();
        }

        public List<string> GetStringsAsList()
        {
            return StringRepresentation;
        }

        public string[] GetStrings()
        {
            return StringRepresentation.ToArray();
        }
    }
}
