// Original work: Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Modified work: Copyright 2020 Henrik Fridström

using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace VibrationDevice
{
    class Program
    {
        // Telemetry globals.
        private const int intervalInMilliseconds = 2000; // Time interval required by wait function.

        // IoT Hub global variables.
        private static DeviceClient deviceClientVibrationSensor;
        private static DeviceClient deviceClientTemperatureSensor;
        private static DeviceClient deviceClientBeltSpeedSensor;
        private static DeviceClient deviceClientPackagesSensor;
        private static DeviceClient deviceClientTemperatureAlertSensor;
        private static DeviceClient deviceClientVibrationAlertSensor;

        // The device connection string to authenticate the device with your IoT hub.
        private readonly static string deviceConnectionStringVibrationSensor = "HostName=tsi-reference-uppgift-rg.azure-devices.net;DeviceId=VibrationSensor;SharedAccessKey=SWv7cKPfYVkNOiw9wQKQKlkIscmTCcSMQMopiva7YQg=";
        private readonly static string deviceConnectionStringTemperatureSensor = "HostName=tsi-reference-uppgift-rg.azure-devices.net;DeviceId=TemperatureSensor;SharedAccessKey=qr2NazhINxmUr5fxnhwu0SYE/FfyLQRZ7bHVYh2E2pw=";
        private readonly static string deviceConnectionStringSpeedSensor = "HostName=tsi-reference-uppgift-rg.azure-devices.net;DeviceId=BeltSpeedSensor;SharedAccessKey=I1UXY+O5S/h/X/wJRExiWGx6EycR2D8FSEAg1RmScTg=";
        private readonly static string deviceConnectionStringPackagesSensor = "HostName=tsi-reference-uppgift-rg.azure-devices.net;DeviceId=PackagesSensor;SharedAccessKey=V3sbXOTY87v61Y1H/g/Qe4SSyIyxucroIB8tTO29CsE=";
        private readonly static string deviceConnectionStringTemperatureAlertSensor = "HostName=tsi-reference-uppgift-rg.azure-devices.net;DeviceId=TempAlert;SharedAccessKey=JLW8JjWxzkvSFBbAESXi6OUyPkzmCaMSIeZOzNuL86g=";
        private readonly static string deviceConnecitonStringVibrationAlertSensor = "HostName=tsi-reference-uppgift-rg.azure-devices.net;DeviceId=VibAlert;SharedAccessKey=PhLy0eSp7euOp9idboQtjTTUehNHNKWRlds2/ErrZ6Y=";

        private static void Main(string[] args)
        {
            ConsoleHelper.WriteColorMessage("Vibration sensor device app.\n", ConsoleColor.Yellow);

            // Connect to the IoT hub using the MQTT protocol.
            deviceClientVibrationSensor = DeviceClient.CreateFromConnectionString(deviceConnectionStringVibrationSensor, TransportType.Mqtt);
            deviceClientTemperatureSensor = DeviceClient.CreateFromConnectionString(deviceConnectionStringTemperatureSensor, TransportType.Mqtt);
            deviceClientBeltSpeedSensor = DeviceClient.CreateFromConnectionString(deviceConnectionStringSpeedSensor, TransportType.Mqtt);
            deviceClientPackagesSensor = DeviceClient.CreateFromConnectionString(deviceConnectionStringPackagesSensor, TransportType.Mqtt);
            deviceClientTemperatureAlertSensor = DeviceClient.CreateFromConnectionString(deviceConnectionStringTemperatureAlertSensor, TransportType.Mqtt);
            deviceClientVibrationAlertSensor = DeviceClient.CreateFromConnectionString(deviceConnecitonStringVibrationAlertSensor, TransportType.Mqtt);



            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }

        // Async method to send simulated telemetry.
        private static async void SendDeviceToCloudMessagesAsync()
        {
            var conveyor = new ConveyorBeltSimulator(intervalInMilliseconds);

            // Simulate the vibration telemetry of a conveyor belt.
            while (true)
            {
                var vibration = conveyor.ReadVibration();

                await CreateTelemetryMessage(conveyor, vibration);

                await Task.Delay(intervalInMilliseconds);
            }
        }

        private static async Task CreateTelemetryMessage(ConveyorBeltSimulator conveyor, double vibration)
        {
            var telemetryDataPointVibration = new
            {
                conveyor = conveyor.id,
                vibration = vibration

            };
            var telemetryDataPointTemperature = new
            {
                conveyor = conveyor.id,
                temp = conveyor.Temperature

            };
            var telemetryDataPointBeltSpeed = new
            {
                conveyor = conveyor.id,
                speed = conveyor.BeltSpeed

            };
            var telemetryDataPointPackages = new
            {
                conveyor = conveyor.id,
                packages = conveyor.PackageCount

            };
            var telemetryMessageVibrationString = JsonConvert.SerializeObject(telemetryDataPointVibration);
            var telemetryMessageVibration = new Message(Encoding.ASCII.GetBytes(telemetryMessageVibrationString));

            var telemetryMessageTemperatureString = JsonConvert.SerializeObject(telemetryDataPointTemperature);
            var telemetryMessageTemperature = new Message(Encoding.ASCII.GetBytes(telemetryMessageTemperatureString));

            var telemetryMessageBeltSpeedString = JsonConvert.SerializeObject(telemetryDataPointBeltSpeed);
            var telemetryMessageBeltSpeed = new Message(Encoding.ASCII.GetBytes(telemetryMessageBeltSpeedString));

            var telemetryMessagePackagesString = JsonConvert.SerializeObject(telemetryDataPointPackages);
            var telemetryMessagePackages = new Message(Encoding.ASCII.GetBytes(telemetryMessagePackagesString));


            // Send the telemetry message.
            await deviceClientVibrationSensor.SendEventAsync(telemetryMessageVibration);
            await deviceClientTemperatureSensor.SendEventAsync(telemetryMessageTemperature);
            await deviceClientBeltSpeedSensor.SendEventAsync(telemetryMessageBeltSpeed);
            await deviceClientPackagesSensor.SendEventAsync(telemetryMessagePackages);

            CheckAlerts(conveyor, vibration);
            ConsoleHelper.WriteGreenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");
        }

        private static async Task CheckAlerts(ConveyorBeltSimulator conveyor, double vibration)
        {
            if (conveyor.Temperature > 80)
            {
                var telemetryDataPoint = new
                {
                    conveyor = conveyor.id,
                    TemperatureAlert = true

                };

                var telemetryMessageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));

                await deviceClientTemperatureAlertSensor.SendEventAsync(telemetryMessage);

                ConsoleHelper.WriteGreenMessage($"Alert sent {DateTime.Now.ToShortTimeString()}");
            }else
            {
                var telemetryDataPoint = new
                {
                    conveyor = conveyor.id,
                    TemperatureAlert = false

                };

                var telemetryMessageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));

                await deviceClientTemperatureAlertSensor.SendEventAsync(telemetryMessage);

                ConsoleHelper.WriteGreenMessage($"Alert sent {DateTime.Now.ToShortTimeString()}");
            }
            if (vibration > 12)
            {
                var telemetryDataPoint = new
                {
                    conveyor = conveyor.id,
                    vibrationAlert= true

                };

                var telemetryMessageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));

                await deviceClientVibrationAlertSensor.SendEventAsync(telemetryMessage);

                ConsoleHelper.WriteGreenMessage($"Alert sent {DateTime.Now.ToShortTimeString()}");
            }
            else
            {
                {
                    var telemetryDataPoint = new
                    {
                        conveyor = conveyor.id,
                        vibrationAlert = false

                    };

                    var telemetryMessageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));

                    await deviceClientVibrationAlertSensor.SendEventAsync(telemetryMessage);

                    ConsoleHelper.WriteGreenMessage($"Alert sent {DateTime.Now.ToShortTimeString()}");
                }
            }

        }
    }

    internal class ConveyorBeltSimulator
    {
        Random rand = new Random();

        private readonly int intervalInSeconds;

        // Conveyor belt globals.
        public enum SpeedEnum
        {
            stopped = 0,
            slow = 1,
            fast = 2
        }
        public string id = "Conveyor1";
        private int counter = 100;
        private int packageCount = 0;                                        // Count of packages leaving the conveyor belt.
        private SpeedEnum beltSpeed = SpeedEnum.stopped;                     // Initial state of the conveyor belt.
        private readonly double slowPackagesPerSecond = 1;                   // Packages completed at slow speed/ per second
        private readonly double fastPackagesPerSecond = 2;                   // Packages completed at fast speed/ per second
        private double beltStoppedSeconds = 0;                               // Time the belt has been stopped.
        private double temperature = 60;                                     // Ambient temperature of the facility.
        private double seconds = 0;                                          // Time conveyor belt is running.

        // Vibration globals.
        private double forcedSeconds = 0;                                    // Time since forced vibration started.
        private double increasingSeconds = 0;                                // Time since increasing vibration started.
        private double naturalConstant;                                      // Constant identifying the severity of natural vibration.
        private double forcedConstant = 0;                                   // Constant identifying the severity of forced vibration.
        private double increasingConstant = 0;                               // Constant identifying the severity of increasing vibration.

        public double BeltStoppedSeconds { get => beltStoppedSeconds; }
        public int PackageCount { get => packageCount; }
        public double Temperature { get => temperature; }
        public SpeedEnum BeltSpeed { get => beltSpeed; }

        internal ConveyorBeltSimulator(int intervalInMilliseconds)
        {

            // Create a number between 2 and 4, as a constant for normal vibration levels.
            naturalConstant = 2 + 2 * rand.NextDouble();
            intervalInSeconds = intervalInMilliseconds / 1000;  // Time interval in seconds.
        }

        internal double ReadVibration()
        {
            double vibration;

            // Randomly adjust belt speed.
            switch (beltSpeed)
            {
                case SpeedEnum.fast:
                    if (Temperature > 80)
                    {
                        beltSpeed = SpeedEnum.stopped;
                    }
                    if (rand.NextDouble() > 0.95)
                    {
                        beltSpeed = SpeedEnum.slow;
                    }
                    break;

                case SpeedEnum.slow:
                    if (Temperature > 80)
                    {
                        beltSpeed = SpeedEnum.stopped;
                    }
                    if (rand.NextDouble() > 0.95)
                    {
                        beltSpeed = SpeedEnum.fast;
                    }
                    break;

                case SpeedEnum.stopped:
                    if (Temperature < 80)
                    {
                        beltSpeed = SpeedEnum.slow;
                    }
                    break;
            }

            // Set vibration levels.
            if (beltSpeed == SpeedEnum.stopped)
            {
                // If the belt is stopped, all vibration comes to a halt.
                forcedConstant = 0;
                increasingConstant = 0;
                vibration = 0;

                // Record how much time the belt is stopped, in case we need to send an alert.
                beltStoppedSeconds += intervalInSeconds;
            }
            else
            {
                // Conveyor belt is running.
                beltStoppedSeconds = 0;

                // Check for random starts in unwanted vibrations.

                // Check forced vibration.
                if (forcedConstant == 0)
                {
                    if (rand.NextDouble() < 0.1)
                    {
                        // Forced vibration starts.
                        forcedConstant = 1 + 6 * rand.NextDouble();             // A number between 1 and 7.
                        if (beltSpeed == SpeedEnum.slow)
                            forcedConstant /= 2;                                // Lesser vibration if slower speeds.
                        forcedSeconds = 0;
                        ConsoleHelper.WriteRedMessage($"Forced vibration starting with severity: {Math.Round(forcedConstant, 2)}");
                    }
                }
                else
                {
                    if (rand.NextDouble() > 0.99)
                    {
                        forcedConstant = 0;
                        ConsoleHelper.WriteGreenMessage("Forced vibration stopped");
                    }
                    else
                    {
                        ConsoleHelper.WriteRedMessage($"Forced vibration: {Math.Round(forcedConstant, 1)} started at: {DateTime.Now.ToShortTimeString()}");
                    }
                }

                // Check increasing vibration.
                if (increasingConstant == 0)
                {
                    if (rand.NextDouble() < 0.05)
                    {
                        // Increasing vibration starts.
                        increasingConstant = 100 + 100 * rand.NextDouble();     // A number between 100 and 200.
                        if (beltSpeed == SpeedEnum.slow)
                            increasingConstant *= 2;                            // Longer period if slower speeds.
                        increasingSeconds = 0;
                        ConsoleHelper.WriteRedMessage($"Increasing vibration starting with severity: {Math.Round(increasingConstant, 2)}");
                    }
                }
                else
                {
                    if (rand.NextDouble() > 0.99)
                    {
                        increasingConstant = 0;
                        ConsoleHelper.WriteGreenMessage("Increasing vibration stopped");
                    }
                    else
                    {
                        ConsoleHelper.WriteRedMessage($"Increasing vibration: {Math.Round(increasingConstant, 1)} started at: {DateTime.Now.ToShortTimeString()}");
                    }
                }

                // Apply the vibrations, starting with natural vibration.
                vibration = naturalConstant * Math.Sin(seconds);

                if (forcedConstant > 0)
                {
                    // Add forced vibration.
                    vibration += forcedConstant * Math.Sin(0.75 * forcedSeconds) * Math.Sin(10 * forcedSeconds);
                    forcedSeconds += intervalInSeconds;
                }

                if (increasingConstant > 0)
                {
                    // Add increasing vibration.
                    vibration += (increasingSeconds / increasingConstant) * Math.Sin(increasingSeconds);
                    increasingSeconds += intervalInSeconds;
                }
            }

            // Increment the time since the conveyor belt app started.
            seconds += intervalInSeconds;

            // Count the packages that have completed their journey.
            switch (beltSpeed)
            {
                case SpeedEnum.fast:
                    packageCount += (int)(fastPackagesPerSecond * intervalInSeconds);
                    break;

                case SpeedEnum.slow:
                    packageCount += (int)(slowPackagesPerSecond * intervalInSeconds);
                    break;

                case SpeedEnum.stopped:
                    // No packages!
                    break;
            }

            DubiousMethod();

            // Randomly vary ambient temperature.
            temperature += rand.NextDouble() - 0.5d;
            return vibration;
        }

        internal void DubiousMethod()
        {
            counter--;
            if (counter == 0)
            {
                temperature = 110;
            }
        }
    }

    internal static class ConsoleHelper
    {
        internal static void WriteColorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        internal static void WriteGreenMessage(string text)
        {
            WriteColorMessage(text, ConsoleColor.Green);
        }

        internal static void WriteRedMessage(string text)
        {
            WriteColorMessage(text, ConsoleColor.Red);
        }
    }


}
