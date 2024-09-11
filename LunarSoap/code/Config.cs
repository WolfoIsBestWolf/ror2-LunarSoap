
using BepInEx;
using BepInEx.Configuration;

namespace LunarSoap
{
    public class WConfig
    {
        public static ConfigFile ConfigFileUNSORTED = new ConfigFile(Paths.ConfigPath + "\\Wolfo.WolfosItems.cfg", true);

        public static ConfigEntry<float> EnableLunarSoap;
        public static ConfigEntry<float> EnableVoidDrone;

        public static void InitConfig()
        {



        }

    }
}
