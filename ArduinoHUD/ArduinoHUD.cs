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
            InfoCycle
        }

        private enum HUDCycle
        {
            WorldInfo = 0,
            PlayerHealth,
            VehicleSpeed,
            VehicleHealth,
            Count
        }

        private enum ToggleFormat
        {
            Timer,
            Arduino
        }

        private struct HUDPreferences
        {
            public String HourFormat;
            public String SpeedFormat;
            public ToggleFormat ToggleFormat; 

            public HUDPreferences(String hourFormat, String speedFormat, String toggleFormat)
            {
                HourFormat = (hourFormat == "24h" || hourFormat == "12h" ? hourFormat : "24h");
                SpeedFormat = (speedFormat == "kmh" || speedFormat == "mph" ? speedFormat : "kmh");
                if (toggleFormat == "ARDUINO")
                {
                    ToggleFormat = ToggleFormat.Arduino;
                }
                else
                {
                    ToggleFormat = ToggleFormat.Timer;
                }
            }

            public Boolean PrefersTimer()
            {
                return ToggleFormat == ToggleFormat.Timer;
            }
        }

        private int Timer = 0;
        private const int MaxRotatingStateLength = 5000;

        private HUDState State = HUDState.Unknown;
        private HUDCycle Cycle = HUDCycle.WorldInfo;
        private Boolean CycleChanged = false;
        private HUDPreferences Preferences;

        private int PlayerHealth = -1;
        private int PlayerArmor = -1;
        private Boolean IsInVehicle = false;
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

            ArduinoInterface.Toggled += ArduinoInterface_Toggled;

            Preferences = new HUDPreferences(
                localSettings.GetValue("Preferences", "HourFormat", "24h"),
                localSettings.GetValue("Preferences", "SpeedFormat", "kmh"),
                localSettings.GetValue("Preferences", "ToggleFormat", "TIMER")
            );

            SetHUDState(HUDState.InfoCycle);

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
                        if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true))
                        {
                            SetHUDState(HUDState.Busted);
                        }
                        else
                        {
                            SetHUDState(HUDState.InfoCycle);
                            CheckCycle();
                            UpdateInfo();

                            if (Preferences.PrefersTimer())
                            {
                                Timer += Interval;
                                CheckTimer();
                            }
                        }
                    }
                    else
                    {
                        SetHUDState(HUDState.Wasted);
                    }
                }
            }
        }

        private void CheckCycle()
        {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle() != IsInVehicle)
            {
                IsInVehicle = player.IsInVehicle();
                if (IsInVehicle)
                {
                    GoToCycle(HUDCycle.VehicleSpeed);
                    CycleChanged = true;
                }
            }

            if (Cycle >= HUDCycle.VehicleSpeed && !IsInVehicle)
            {
                GoToCycle(HUDCycle.WorldInfo);
            }
        }

        private void UpdateInfo()
        {
            Ped player = Game.Player.Character;
            if (player.Exists() && player.IsAlive)
            {
                Vehicle vehicle = (player.IsInVehicle() ? player.CurrentVehicle : null);

                switch (Cycle)
                {
                    case HUDCycle.WorldInfo:
                        DateTime currentTime = new DateTime(World.CurrentDayTime.Ticks);
                        ArduinoInterface.SetCursor(0, 0);
                        ArduinoInterface.Print((currentTime.ToString(Preferences.HourFormat == "12h" ? "h:mmtt" : "HH:mm").ToLower() + " - " + World.Weather).MinLength(12));
                        ArduinoInterface.SetCursor(0, 1);
                        ArduinoInterface.Print(World.GetZoneName(player.Position).MinLength(12));
                        break;

                    case HUDCycle.PlayerHealth:
                        if (player.Health != PlayerHealth)
                        {
                            PlayerHealth = player.Health;
                            ArduinoInterface.SetCursor(0, 0);
                            ArduinoInterface.Print(("Health: " + PlayerHealth.ToString() + "%").MinLength(12));
                        }

                        if (player.Armor != PlayerArmor)
                        {
                            PlayerArmor = player.Armor;
                            ArduinoInterface.SetCursor(0, 1);
                            ArduinoInterface.Print((" Armor: " + PlayerArmor.ToString() + "%").MinLength(12));
                        }

                        break;

                    case HUDCycle.VehicleSpeed:
                        if (vehicle != null)
                        {
                            if (CycleChanged)
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
                        }
                        else
                        {
                            GoToCycle(HUDCycle.WorldInfo);
                        }

                        break;

                    case HUDCycle.VehicleHealth:
                        if (vehicle != null)
                        {
                            if (vehicle.BodyHealth != VehicleBodyHealth)
                            {
                                VehicleBodyHealth = vehicle.BodyHealth;
                                ArduinoInterface.SetCursor(0, 0);
                                ArduinoInterface.Print(("  Body: " + Math.Round(VehicleBodyHealth / 10).ToString() + "%").MinLength(12));
                            }

                            if (vehicle.EngineHealth != VehicleEngineHealth)
                            {
                                VehicleEngineHealth = vehicle.EngineHealth;
                                ArduinoInterface.SetCursor(0, 1);
                                ArduinoInterface.Print(("Engine: " + Math.Round(VehicleEngineHealth / 10).ToString() + "%").MinLength(12));
                            }
                        }
                        else
                        {
                            GoToCycle(HUDCycle.WorldInfo);
                        }

                        break;
                }
            }

            CycleChanged = false;
        }

        private void GoToNextInCycle()
        {
            GoToCycle(Cycle + 1 == HUDCycle.Count ? 0 : Cycle + 1);
        }

        private void GoToCycle(HUDCycle newCycle, Boolean forced = false)
        {
            if (newCycle != Cycle || forced)
            {
                ArduinoInterface.Clear();

                Timer = 0;
                PlayerHealth = -1;
                PlayerArmor = -1;
                VehicleBodyHealth = -1;
                VehicleEngineHealth = -1;

                Cycle = newCycle;
                CycleChanged = true;
            }
        }

        private Boolean SetHUDState(HUDState newState)
        {
            if (newState != State)
            {
                ArduinoInterface.Clear();

                State = newState;
                switch (State)
                {
                    case HUDState.Wasted:
                        ArduinoInterface.Print("WASTED");
                        break;

                    case HUDState.Busted:
                        ArduinoInterface.Print("BUSTED");
                        break;

                    case HUDState.InfoCycle:
                        GoToCycle(Game.Player.Character.IsInVehicle() ? HUDCycle.VehicleSpeed : HUDCycle.WorldInfo, true);
                        break;
                }

                return true;
            }

            return false;
        }

        private void CheckTimer()
        {
            if (Timer >= MaxRotatingStateLength)
            {
                GoToNextInCycle();
            }
        }

        private void ArduinoInterface_Toggled()
        {
            GoToNextInCycle();
        }
    }
}