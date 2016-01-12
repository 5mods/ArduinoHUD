using System;
using GTA;
using GTA.Native;

namespace ArduinoHUD
{
    static class WorldExtension
    {
        static String[] WeatherNames = { "EXTRASUNNY", "CLEAR", "CLOUDS", "SMOG", "FOGGY", "OVERCAST", "RAIN", "THUNDER", "CLEARING", "NEUTRAL", "SNOW", "BLIZZARD", "SNOWLIGHT", "XMAS" };

        public static Weather Weather
        {
            get
            {
                for (int i = 0; i < WeatherNames.Length; i++) 
                {
                    int weatherHash = Function.Call<int>((Hash)0x564B884A05EC45A3);
                    if (weatherHash == Function.Call<int>(Hash.GET_HASH_KEY, WeatherNames[i])) {
                        return (Weather)i;
                    }
                }

                return Weather.Christmas;
            }
        }
    }
}
