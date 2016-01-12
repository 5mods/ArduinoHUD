using System;
using System.IO.Ports;

namespace ArduinoHUD
{
    enum SerialCommand
    {
        StartCommand = 10,
        Clear = 1,
        SetCursor = 2,
        SetLEDCount = 3
    }
    static class ArduinoInterface
    {
        static SerialPort Port;

        public static Boolean IsAvailable()
        {
            return Port.IsOpen;
        }

        public static void OpenPort(String portName, int baudRate)
        {
            Port = new SerialPort(portName, baudRate);
            Port.Open();
        }

        public static void Print(String str)
        {
            Port.Write(str);
        }

        public static void Clear()
        {
            byte[] commandBytes = new byte[2];
            commandBytes[0] = Convert.ToByte(SerialCommand.StartCommand);
            commandBytes[1] = Convert.ToByte(SerialCommand.Clear);
            SendSerialCommand(commandBytes);
        }

        public static void SetCursor(int column, int row)
        {
            byte[] commandBytes = new byte[4];
            commandBytes[0] = Convert.ToByte(SerialCommand.StartCommand);
            commandBytes[1] = Convert.ToByte(SerialCommand.SetCursor);
            commandBytes[2] = Convert.ToByte(column);
            commandBytes[3] = Convert.ToByte(row);
            SendSerialCommand(commandBytes);
        }

        public static void SetLEDCount(int count)
        {
            byte[] commandBytes = new byte[3];
            commandBytes[0] = Convert.ToByte(SerialCommand.StartCommand);
            commandBytes[1] = Convert.ToByte(SerialCommand.SetLEDCount);
            commandBytes[2] = Convert.ToByte(count);
            SendSerialCommand(commandBytes);
        }

        public static void SendSerialCommand(byte[] commandBytes)
        {
            if (Port.IsOpen)
            {
                Port.Write(commandBytes, 0, commandBytes.Length);
            }
        }
    }
}
