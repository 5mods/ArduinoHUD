using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoHUD
{
    public static class StringExtension
    {
        public static String MinLength(this String value, int minLength)
        {
            while (value.Length < minLength)
            {
                value += " ";
            }

            return value;
        }
    }
}
