using GTA;
using GTA.Native;
using System;

namespace ArduinoHUD
{
    public class ArduinoHUD : Script
    {
        private enum HUDState
        {
            Unknown = -1,
            Wasted,
            Busted,
            WorldInfo,
            PlayerHealth,
            VehicleSpeed,
            VehicleHealth
        }
        private struct HUDPreferences
        {
            public String HourFormat;
            public String SpeedFormat;

            public HUDPreferences(String hourFormat, String speedFormat)
            {
                HourFormat = (hourFormat == "24h" || hourFormat == "12h" ? hourFormat : "24h");
                SpeedFormat = (speedFormat == "kmh" || speedFormat == "mph" ? speedFormat : "kmh");
            }
        }

        private int Timer = 0;
        private const int MaxRotatingStateLength = 5000;
        private HUDState state = HUDState.Unknown;
        private HUDPreferences Preferences;

        private int PlayerHealth = -1;
        private int PlayerArmor = -1;
        private float VehicleBodyHealth = -1;
        private float VehicleEngineHealth = -1;
        private int WantedLevel = -1;

        public ArduinoHUD()
        {
            ScriptSettings localSettings = ScriptSettings.Load(".\\scripts\\ArduinoHUD.ini");

            ArduinoInterface.OpenPort(
                localSettings.GetValue("Settings", "COMPort", "COM3"), 
                localSettings.GetValue<int>("Settings", "BaudRate", 9600)
            );

            Preferences = new HUDPreferences(
                localSettings.GetValue("Preferences", "HourFormat", "24h"),
                localSettings.GetValue("Preferences", "SpeedFormat", "kmh")
            );

            SetHUDState(HUDState.WorldInfo);

            Interval = 500;
            Tick += OnTick;
        }

        private void OnTick(object o, EventArgs e)
        {
            if (ArduinoInterface.IsAvailable())
            {
                if (WantedLevel != Game.Player.WantedLevel)
                {
                    WantedLevel = Game.Player.WantedLevel;
                    ArduinoInterface.SetLEDCount(WantedLevel);
                }

                Ped player = Game.Player.Character;
                if (player.Exists())
                {
                    if (player.IsAlive)
                    {
                        if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, player, true))
                        {
                            SetHUDState(HUDState.Busted);
                        }
                        else if (player.IsInVehicle())
                        {
                            Vehicle vehicle = player.CurrentVehicle;

                            Boolean stateChanged = false;
                            if (Timer >= MaxRotatingStateLength)
                            {
                                if (state == HUDState.VehicleHealth)
                                {
                                    SetHUDState(HUDState.VehicleSpeed);
                                }
                                else
                                {
                                    SetHUDState(HUDState.VehicleHealth);
                                }

                                stateChanged = true;
                            }
                            else if (state != HUDState.VehicleHealth && state != HUDState.VehicleSpeed)
                            {
                                stateChanged = SetHUDState(HUDState.VehicleSpeed);
                            }

                            switch (state)
                            {
                                case HUDState.VehicleSpeed:
                                    if (stateChanged)
                                    {
                                        ArduinoInterface.SetCursor(0, 0);
                                        ArduinoInterface.Print(vehicle.FriendlyName);
                                    }

                                    ArduinoInterface.SetCursor(0, 1);
                                    float speed = vehicle.Speed;
                                    if (Preferences.SpeedFormat == "mph")
                                    {
                                        speed *= 2.236936292054f;
                                    }
                                    else
                                    {
                                        speed *= 3.6f;
                                    }

                                    ArduinoInterface.Print("Speed: " + (Math.Round(speed).ToString() + Preferences.SpeedFormat).MinLength(6));
                                    break;

                                case HUDState.VehicleHealth:
                                    if (vehicle.BodyHealth != VehicleBodyHealth)
                                    {
                                        VehicleBodyHealth = vehicle.BodyHealth;
                                        ArduinoInterface.SetCursor(8, 0);
                                        ArduinoInterface.Print((Math.Round(VehicleBodyHealth / 10).ToString() + "%").MinLength(4));
                                    }

                                    if (vehicle.EngineHealth != VehicleEngineHealth)
                                    {
                                        VehicleEngineHealth = vehicle.EngineHealth;
                                        ArduinoInterface.SetCursor(8, 1);
                                        ArduinoInterface.Print((Math.Round(VehicleEngineHealth / 10).ToString() + "%").MinLength(4));
                                    }

                                    break;
                            }
                        }
                        else
                        {
                            if (Timer >= MaxRotatingStateLength)
                            {
                                if (state == HUDState.WorldInfo)
                                {
                                    SetHUDState(HUDState.PlayerHealth);
                                }
                                else
                                {
                                    SetHUDState(HUDState.WorldInfo);
                                }
                            }
                            else if (state != HUDState.WorldInfo && state != HUDState.PlayerHealth)
                            {
                                SetHUDState(HUDState.WorldInfo);
                            }

                            switch (state)
                            {
                                case HUDState.WorldInfo:
                                    DateTime currentTime = new DateTime(World.CurrentDayTime.Ticks);
                                    ArduinoInterface.SetCursor(0, 0);
                                    ArduinoInterface.Print((currentTime.ToString(Preferences.HourFormat == "12h" ? "h:mmtt" : "HH:mm").ToLower() + " - " + WorldExtension.Weather).MinLength(12));
                                    ArduinoInterface.SetCursor(0, 1);
                                    ArduinoInterface.Print(World.GetZoneName(player.Position).MinLength(12));
                                    break;

                                case HUDState.PlayerHealth:
                                    if (player.Health != PlayerHealth)
                                    {
                                        PlayerHealth = player.Health;
                                        ArduinoInterface.SetCursor(8, 0);
                                        ArduinoInterface.Print((PlayerHealth.ToString() + "%").MinLength(4));
                                    }

                                    if (player.Armor != PlayerArmor)
                                    {
                                        PlayerArmor = player.Armor;
                                        ArduinoInterface.SetCursor(8, 1);
                                        ArduinoInterface.Print((PlayerArmor.ToString() + "%").MinLength(4));
                                    }

                                    break;
                            }
                        }
                    }
                    else
                    {
                        SetHUDState(HUDState.Wasted);
                    }
                }
            }

            Timer += Interval;
        }

        private Boolean SetHUDState(HUDState newState)
        {
            if (newState != state)
            {
                ArduinoInterface.Clear();
                Timer = 0;

                state = newState;
                switch (state)
                {
                    case HUDState.Wasted:
                        ArduinoInterface.Print("WASTED");
                        break;

                    case HUDState.PlayerHealth:
                        PlayerHealth = -1;
                        PlayerArmor = -1;
                        ArduinoInterface.SetCursor(0, 0);
                        ArduinoInterface.Print("Health: 0%");
                        ArduinoInterface.SetCursor(0, 1);
                        ArduinoInterface.Print(" Armor: 0%");
                        break;

                    case HUDState.VehicleHealth:
                        VehicleBodyHealth = -1;
                        VehicleEngineHealth = -1;
                        ArduinoInterface.SetCursor(0, 0);
                        ArduinoInterface.Print("  Body: 0%");
                        ArduinoInterface.SetCursor(0, 1);
                        ArduinoInterface.Print("Engine: 0%");
                        break;
                }

                return true;
            }

            return false;
        }
    }
}